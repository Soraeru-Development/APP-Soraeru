using Soraeru.ClientLogic.Legal;

namespace Soraeru.Pages;

[QueryProperty(nameof(Doc), "doc")]
public partial class LegalDocumentPage : ContentPage
{
    string? _doc;

    public LegalDocumentPage()
    {
        InitializeComponent();
    }

    public string Doc
    {
        set
        {
            _doc = value;
            ApplyDocument();
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplyDocument();
    }

    void ApplyDocument()
    {
        var (title, body) = LegalDocuments.Resolve(_doc);
        Title = title;
        BodyLabel.Text = body.Trim();
    }
}
