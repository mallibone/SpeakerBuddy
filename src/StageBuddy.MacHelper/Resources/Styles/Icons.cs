namespace StageBuddy.MacHelper.Resources.Styles;

/// <summary>
/// Glyph constants for the bundled Fluent UI System Icons font (Filled, 24px variants).
/// Mirrored subset of the iOS app's Icons class — keep codepoints in sync.
/// </summary>
static class Icons
{
    public const string FontFamily = "FluentIcons";

    public const string Copy = "\uF32C";        // copy
    public const string Check = "\uF295";       // checkmark
    public const string Warning = "\uF882";     // warning
    public const string Presenter = "\uF62C";   // presenter

    public static MauiControls.FontImageSource Glyph(string glyph, Color color, double size = 20)
        => new() { FontFamily = FontFamily, Glyph = glyph, Color = color, Size = size };
}
