using Soraeru.ClientLogic.Notebook;
using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class NotebookListPage : ContentPage
{
    private readonly LocalNotebookService _notebook;
    private readonly IAuthSessionStore _session;
    private string _search = string.Empty;
    private string? _languageFilter;

    public NotebookListPage(LocalNotebookService notebook, IAuthSessionStore session)
    {
        InitializeComponent();
        _notebook = notebook;
        _session = session;
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

    async void OnFilterAllClicked(object? sender, EventArgs e)
    {
        _languageFilter = null;
        HighlightFilter(FilterAllButton);
        await ReloadAsync();
    }

    async void OnFilterEnClicked(object? sender, EventArgs e)
    {
        _languageFilter = "en";
        HighlightFilter(FilterEnButton);
        await ReloadAsync();
    }

    async void OnFilterJaClicked(object? sender, EventArgs e)
    {
        _languageFilter = "ja";
        HighlightFilter(FilterJaButton);
        await ReloadAsync();
    }

    async void OnFilterThClicked(object? sender, EventArgs e)
    {
        _languageFilter = "th";
        HighlightFilter(FilterThButton);
        await ReloadAsync();
    }

    async void OnFilterOtherClicked(object? sender, EventArgs e)
    {
        _languageFilter = "__other__";
        HighlightFilter(FilterOtherButton);
        await ReloadAsync();
    }

    void HighlightFilter(Button active)
    {
        foreach (var button in new[]
                 {
                     FilterAllButton, FilterEnButton, FilterJaButton, FilterThButton, FilterOtherButton
                 })
        {
            button.Style = (Style)Application.Current!.Resources[
                ReferenceEquals(button, active) ? "PrimaryButton" : "SecondaryButton"];
        }
    }

    async Task ReloadAsync()
    {
        try
        {
            var signedIn = await _session.HasSessionAsync();
            IEnumerable<LocalWordCard> cards = await _notebook.ListAsync();
            if (!string.IsNullOrEmpty(_languageFilter))
            {
                cards = _languageFilter == "__other__"
                    ? cards.Where(c => c.DetectedLanguage is not ("en" or "ja" or "th"))
                    : cards.Where(c =>
                        string.Equals(c.DetectedLanguage, _languageFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(_search))
            {
                cards = cards.Where(c =>
                    c.SourceText.Contains(_search, StringComparison.OrdinalIgnoreCase)
                    || c.SelectedMnemonic.Contains(_search, StringComparison.OrdinalIgnoreCase)
                    || c.MeaningZh.Contains(_search, StringComparison.OrdinalIgnoreCase));
            }

            var list = cards.ToList();
            CardsHost.Children.Clear();

            if (list.Count == 0)
            {
                if (!signedIn)
                {
                    ShowEmpty(
                        "登入後可收藏單字卡",
                        "未登入時無法新增；若裝置上曾有本機單字本，登入同一帳號後仍可維護。");
                }
                else
                {
                    ShowEmpty(
                        "還沒有單字卡",
                        "分析第一個外語單字，存成可隨時查看的近似音卡片（離線亦可寫入本機）。");
                }

                return;
            }

            EmptyState.IsVisible = false;
            FilledState.IsVisible = true;

            foreach (var card in list)
            {
                var cardId = card.Id;
                var border = new Border
                {
                    Style = (Style)Application.Current!.Resources["CardBorder"],
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children =
                        {
                            new Label
                            {
                                Text = card.SourceText,
                                FontSize = 20,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = (Color)Application.Current.Resources["OnSurface"]
                            },
                            new Label
                            {
                                Text = $"{card.MeaningZh}｜{card.SelectedMnemonic}｜{card.DetectedLanguage}",
                                Style = (Style)Application.Current.Resources["BodyLabel"]
                            }
                        }
                    }
                };
                border.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () =>
                        await Routes.GoAsync($"{Routes.NotebookDetail}?cardId={cardId:D}"))
                });
                CardsHost.Children.Add(border);
            }
        }
        catch (Exception ex)
        {
            ShowEmpty("讀取失敗", ex.Message);
        }
    }

    void ShowEmpty(string title, string body)
    {
        FilledState.IsVisible = false;
        EmptyState.IsVisible = true;
        EmptyTitleLabel.Text = title;
        EmptyBodyLabel.Text = body;
        CardsHost.Children.Clear();
    }

    async void OnGoInputClicked(object? sender, EventArgs e) =>
        await Routes.GoAsync(Routes.WordInput);
}
