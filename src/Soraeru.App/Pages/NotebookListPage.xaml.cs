using Microsoft.Maui.Controls.Shapes;
using Soraeru.ClientLogic.Notebook;
using Soraeru.ClientLogic.Tts;
using Soraeru.Languages;
using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class NotebookListPage : ContentPage
{
    private readonly LocalNotebookService _notebook;
    private readonly IAuthSessionStore _session;
    private readonly IFormalTtsService _tts;
    private string _search = string.Empty;
    private string? _languageFilter;

    public NotebookListPage(
        LocalNotebookService notebook,
        IAuthSessionStore session,
        IFormalTtsService tts)
    {
        InitializeComponent();
        _notebook = notebook;
        _session = session;
        _tts = tts;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ReloadAsync();
    }

    async void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _search = e.NewTextValue?.Trim() ?? string.Empty;
        await ReloadAsync();
    }

    async void OnBackClicked(object? sender, EventArgs e) =>
        await Routes.GoToMainTabAsync(Routes.Home);

    async void OnProfileClicked(object? sender, EventArgs e) =>
        await Routes.GoToMainTabAsync(Routes.Settings);

    async void OnGoInputClicked(object? sender, EventArgs e) =>
        await Routes.GoAsync(Routes.WordInput);

    async Task ReloadAsync()
    {
        try
        {
            var signedIn = await _session.HasSessionAsync();
            var allCards = (await _notebook.ListAsync()).ToList();

            RebuildFilterChips(allCards);

            IEnumerable<LocalWordCard> cards = allCards;
            if (!string.IsNullOrEmpty(_languageFilter))
            {
                var filter = _languageFilter;
                cards = cards.Where(c =>
                    string.Equals(
                        SourceLanguageCatalog.Normalize(c.DetectedLanguage),
                        filter,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(_search))
            {
                cards = cards.Where(c =>
                    c.SourceText.Contains(_search, StringComparison.OrdinalIgnoreCase)
                    || c.SelectedMnemonic.Contains(_search, StringComparison.OrdinalIgnoreCase)
                    || c.MeaningZh.Contains(_search, StringComparison.OrdinalIgnoreCase)
                    || c.Pronunciation.Contains(_search, StringComparison.OrdinalIgnoreCase));
            }

            var list = cards.ToList();
            CardsHost.Children.Clear();

            if (allCards.Count == 0)
            {
                if (!signedIn)
                {
                    ShowEmpty(
                        "登入後可收藏單字卡",
                        "未登入時無法新增；若裝置上曾有本機單字本，登入同一帳號後仍可維護。",
                        showCta: true);
                }
                else
                {
                    ShowEmpty(
                        "還沒有單字卡",
                        "開始建立你的專屬單字庫吧！",
                        showCta: true);
                }

                return;
            }

            if (list.Count == 0)
            {
                ShowEmpty(
                    "沒有符合的單字卡",
                    "試試其他關鍵字或語言篩選。",
                    showCta: false);
                return;
            }

            EmptyState.IsVisible = false;
            FilledState.IsVisible = true;

            foreach (var card in list)
                CardsHost.Children.Add(BuildCardView(card));
        }
        catch (Exception ex)
        {
            ShowEmpty("讀取失敗", ex.Message, showCta: false);
        }
    }

    void RebuildFilterChips(IReadOnlyList<LocalWordCard> allCards)
    {
        var languages = SourceLanguageCatalog.PresentInLibrary(allCards.Select(c => c.DetectedLanguage));

        if (_languageFilter is not null
            && languages.All(l => !string.Equals(l.Code, _languageFilter, StringComparison.OrdinalIgnoreCase)))
        {
            _languageFilter = null;
        }

        FilterChipsHost.Children.Clear();

        FilterChipsHost.Children.Add(CreateFilterChip(
            label: "全部",
            iconGlyph: null,
            languageCode: null,
            isSelected: _languageFilter is null));

        foreach (var language in languages)
        {
            FilterChipsHost.Children.Add(CreateFilterChip(
                label: language.ChipLabel,
                iconGlyph: language.IconGlyph,
                languageCode: language.Code,
                isSelected: string.Equals(_languageFilter, language.Code, StringComparison.OrdinalIgnoreCase)));
        }
    }

    View CreateFilterChip(string label, string? iconGlyph, string? languageCode, bool isSelected)
    {
        var styleKey = isSelected ? "FilterChipSelected" : "FilterChip";
        var style = (Style)Application.Current!.Resources[styleKey];

        var text = string.IsNullOrEmpty(iconGlyph) ? label : $"{iconGlyph} {label}";
        var button = new Button
        {
            Text = text,
            Style = style,
            AutomationId = languageCode is null ? "filter_all" : $"filter_{languageCode}"
        };

        button.Clicked += async (_, _) =>
        {
            _languageFilter = languageCode;
            await ReloadAsync();
        };

        return button;
    }

    View BuildCardView(LocalWordCard card)
    {
        var lang = SourceLanguageCatalog.Resolve(card.DetectedLanguage);
        var resources = Application.Current!.Resources;
        var onSurface = (Color)resources["OnSurface"];
        var onSurfaceVariant = (Color)resources["OnSurfaceVariant"];
        var outlineVariant = (Color)resources["OutlineVariant"];
        var surfaceLow = (Color)resources["SurfaceContainerLow"];
        var badgeBg = ResolveColor(lang.BadgeBackgroundKey, (Color)resources["SurfaceContainerHigh"]);
        var badgeFg = ResolveColor(lang.BadgeForegroundKey, onSurfaceVariant);

        var badge = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 4 },
            BackgroundColor = badgeBg,
            Padding = new Thickness(8, 2),
            Content = new Label
            {
                Text = lang.BadgeCode,
                TextColor = badgeFg,
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                VerticalTextAlignment = TextAlignment.Center
            }
        };

        var title = new Label
        {
            Text = FormatCardTitle(card),
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = onSurface,
            LineBreakMode = LineBreakMode.WordWrap,
            VerticalTextAlignment = TextAlignment.Center
        };

        var speaker = new ImageButton
        {
            Source = "ic_volume_up.png",
            BackgroundColor = Colors.Transparent,
            WidthRequest = 36,
            HeightRequest = 36,
            Padding = 4,
            Aspect = Aspect.AspectFit
        };
        SemanticProperties.SetDescription(speaker, "播放發音");
        speaker.Clicked += async (_, _) =>
        {
            var play = await _tts.SpeakFormalSourceAsync(card.SourceText, card.DetectedLanguage);
            if (!play.Success)
                await DisplayAlertAsync("播放", play.Message ?? FormalTtsMessages.SpeakFailed, "了解");
        };

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(GridLength.Star),
                new(GridLength.Auto)
            },
            Margin = new Thickness(0, 0, 0, 8)
        };
        var titleRow = new HorizontalStackLayout
        {
            Spacing = 8,
            Children = { badge, title }
        };
        header.Add(titleRow, 0);
        header.Add(speaker, 1);

        var meaning = BuildMeaningRow(card.MeaningZh, outlineVariant, onSurfaceVariant);

        var mnemonicHeader = new HorizontalStackLayout
        {
            Spacing = 6,
            Margin = new Thickness(0, 0, 0, 4),
            Children =
            {
                new Image
                {
                    Source = "ic_lightbulb.png",
                    HeightRequest = 16,
                    WidthRequest = 16,
                    Aspect = Aspect.AspectFit,
                    VerticalOptions = LayoutOptions.Center
                },
                new Label
                {
                    Text = "我的諧音",
                    TextColor = onSurfaceVariant,
                    FontSize = 13,
                    VerticalTextAlignment = TextAlignment.Center
                }
            }
        };

        var mnemonicBody = new Label
        {
            Text = string.IsNullOrWhiteSpace(card.SelectedMnemonic)
                ? "（尚未填寫諧音）"
                : card.SelectedMnemonic,
            TextColor = onSurface,
            FontSize = 14,
            FontAttributes = FontAttributes.Italic,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var mnemonicBox = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = surfaceLow,
            Padding = 12,
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children = { mnemonicHeader, mnemonicBody }
            }
        };

        var cardId = card.Id;
        var border = new Border
        {
            Style = (Style)resources["CardBorder"],
            Content = new VerticalStackLayout
            {
                Spacing = 0,
                Children = { header, meaning, mnemonicBox }
            }
        };
        border.Shadow = new Shadow
        {
            Brush = Colors.Black,
            Offset = new Point(0, 2),
            Radius = 8,
            Opacity = 0.05f
        };
        border.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
                await Routes.GoAsync($"{Routes.NotebookDetail}?cardId={cardId:D}"))
        });

        return border;
    }

    static View BuildMeaningRow(string meaningZh, Color accent, Color textColor) =>
        new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(new GridLength(2)),
                new(GridLength.Star)
            },
            ColumnSpacing = 8,
            Margin = new Thickness(0, 0, 0, 12),
            Children =
            {
                ColoredColumn(new BoxView { Color = accent }, 0),
                ColoredColumn(
                    new Label
                    {
                        Text = meaningZh,
                        TextColor = textColor,
                        FontSize = 16,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    1)
            }
        };

    static T ColoredColumn<T>(T view, int column) where T : BindableObject
    {
        Grid.SetColumn(view, column);
        return view;
    }

    static string FormatCardTitle(LocalWordCard card)
    {
        if (string.IsNullOrWhiteSpace(card.Pronunciation)
            || string.Equals(card.Pronunciation, card.SourceText, StringComparison.OrdinalIgnoreCase))
            return card.SourceText;

        return $"{card.SourceText} ({card.Pronunciation})";
    }

    static Color ResolveColor(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
            return color;
        return fallback;
    }

    void ShowEmpty(string title, string body, bool showCta)
    {
        FilledState.IsVisible = false;
        EmptyState.IsVisible = true;
        EmptyTitleLabel.Text = title;
        EmptyBodyLabel.Text = body;
        EmptyCtaButton.IsVisible = showCta;
        CardsHost.Children.Clear();
    }
}
