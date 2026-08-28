namespace Soraeru.Languages;

/// <summary>
/// Compact source-language selector backed by a MAUI <see cref="Picker"/>.
/// Populates once; defers change callbacks so Android picker dialogs can close safely.
/// </summary>
public sealed class SourceLanguageDropdownPicker
{
    readonly Picker _picker;
    readonly Action<string> _onChanged;
    bool _suppressSelection;
    string _selectedCode = "auto";
    IReadOnlyList<string> _codes = [];

    public SourceLanguageDropdownPicker(Picker picker, Action<string> onChanged)
    {
        _picker = picker;
        _onChanged = onChanged;
        _picker.SelectedIndexChanged += OnSelectedIndexChanged;
        EnsureItems();
        SetSelectedCode("auto", notify: false);
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
        var index = IndexOfCode(normalized);
        _suppressSelection = true;
        try
        {
            _picker.SelectedIndex = index < 0 ? 0 : index;
        }
        finally
        {
            _suppressSelection = false;
        }

        if (notify)
            _onChanged(_selectedCode);
    }

    void EnsureItems()
    {
        var entries = SourceLanguageCatalog.Search(null);
        _codes = entries.Select(e => e.Code).ToList();

        _suppressSelection = true;
        try
        {
            _picker.Items.Clear();
            foreach (var entry in entries)
                _picker.Items.Add(SourceLanguageCatalog.FormatPickerLabel(entry.Code));
        }
        finally
        {
            _suppressSelection = false;
        }
    }

    void OnSelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_suppressSelection)
            return;

        var index = _picker.SelectedIndex;
        if (index < 0 || index >= _codes.Count)
            return;

        var code = _codes[index];
        if (string.Equals(code, _selectedCode, StringComparison.OrdinalIgnoreCase))
            return;

        _selectedCode = code;
        _picker.Dispatcher.Dispatch(() => _onChanged(_selectedCode));
    }

    int IndexOfCode(string code) =>
        _codes
            .Select((value, index) => (value, index))
            .FirstOrDefault(x => string.Equals(x.value, code, StringComparison.OrdinalIgnoreCase))
            .index;
}
