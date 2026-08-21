namespace Soraeru.Services.Local;

/// <summary>
/// Packaged <c>tessdata_fast</c> languages under <c>Resources/Raw/tessdata</c>
/// (MauiAsset LogicalName flattened to <c>*.traineddata</c>).
/// </summary>
public static class TessdataCatalog
{
    /// <summary>All shipped tessdata_fast packs (ML Kit coverage + Tesseract-only scripts).</summary>
    public static readonly string[] AllTrainedDataFiles =
    [
        "eng.traineddata",
        "jpn.traineddata",
        "kor.traineddata",
        "tha.traineddata",
        "mya.traineddata",
        "lao.traineddata",
        "khm.traineddata",
        "ara.traineddata",
        "bod.traineddata",
        "hin.traineddata",
        "nep.traineddata",
        "chi_tra.traineddata",
        "chi_sim.traineddata",
        "fil.traineddata",
        "vie.traineddata",
        "rus.traineddata",
    ];

    /// <summary>
    /// Scripts without ML Kit on-device modules — primary Tesseract route.
    /// </summary>
    public static readonly string TesseractPrimaryLanguages =
        "tha+mya+lao+khm+ara+bod+rus+nep";

    /// <summary>
    /// Broader Tesseract fallback when ML Kit returned empty (includes CJK / Latin / Devanagari packs).
    /// </summary>
    public static readonly string TesseractBroadFallbackLanguages =
        "eng+jpn+kor+chi_tra+chi_sim+hin+fil+vie+" + TesseractPrimaryLanguages;
}
