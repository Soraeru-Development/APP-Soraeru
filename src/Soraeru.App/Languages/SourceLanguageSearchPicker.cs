using Soraeru.ClientLogic.Languages;

namespace Soraeru.Languages;

/// <summary>
/// Binds SearchBar + CollectionView to <see cref="SourceLanguageCatalog"/> (App wrapper).
/// </summary>
public sealed class SourceLanguageSearchPicker
{
    readonly SearchBar _search;
    readonly CollectionView _list;
    readonly Label _selectedLabel;
    readonly Action<string> _onChanged;
    bool _suppressSelection;
    string _selectedCode = "auto";

    public SourceLanguageSearchPicker(
        SearchBar search,
        CollectionView list,
        Label selectedLabel,
        Action<string> onChanged)
    {
        _search = search;
        _list = list;
        _selectedLabel = selectedLabel;
        _onChanged = onChanged;

        _search.TextChanged += (_, _) => RefreshList();
        _list.SelectionChanged += OnSelectionChanged;
        RefreshList();
        UpdateSelectedLabel();
    }

    public string SelectedCode => _selectedCode;

    public void SetSelectedCode(string? code, bool notify)
    {
        var normalized = string.IsNullOrWhiteSpace(code)
            ? "auto"
            : SourceLanguageCatalog.Normalize(code);
        if (string.Equals(normalized, "und", StringComparison.OrdinalIgnoreCase))
            normalized = "auto";

        _selectedCode = normalized;
        UpdateSelectedLabel();
        HighlightSelected();
        if (notify)
            _onChanged(_selectedCode);
    }

    void RefreshList()
    {
        var items = SourceLanguageCatalog.Search(_search.Text)
            .Select(e => new LanguageRow(
                e.Code,
                SourceLanguageCatalog.FormatPickerLabel(e.Code)))
            .ToList();

        _suppressSelection = true;
        _list.ItemsSource = items;
        HighlightSelected();
        _suppressSelection = false;
    }

    void HighlightSelected()
    {
        if (_list.ItemsSource is not IEnumerable<LanguageRow> rows)
            return;

        var match = rows.FirstOrDefault(r =>
            string.Equals(r.Code, _selectedCode, StringComparison.OrdinalIgnoreCase));
        _list.SelectedItem = match;
    }

    void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelection)
            return;
        if (e.CurrentSelection.FirstOrDefault() is not LanguageRow row)
            return;

        _selectedCode = row.Code;
        UpdateSelectedLabel();
        _onChanged(_selectedCode);
    }

    void UpdateSelectedLabel() =>
        _selectedLabel.Text = $"目前：{SourceLanguageCatalog.FormatPickerLabel(_selectedCode)}";

    public sealed record LanguageRow(string Code, string Display);
}
