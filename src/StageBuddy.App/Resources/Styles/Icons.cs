namespace StageBuddy.App.Resources.Styles;

/// <summary>
/// Glyph constants for the bundled Fluent UI System Icons font (Filled, 24px variants).
/// Codepoints come from FluentSystemIcons-Filled.json in microsoft/fluentui-system-icons (MIT).
/// </summary>
static class Icons
{
    public const string FontFamily = "FluentIcons";

    public const string Import = "\uE4E8";      // document_add
    public const string Settings = "\uF6B3";    // settings
    public const string Copy = "\uF32C";        // copy
    public const string Play = "\uF610";        // play
    public const string Next = "\uF182";        // arrow_right
    public const string Back = "\uF15C";        // arrow_left
    public const string Qr = "\uF63F";          // qr_code
    public const string Warning = "\uF882";     // warning
    public const string Check = "\uF295";       // checkmark
    public const string Timer = "\uF840";       // timer
    public const string Mic = "\uE808";         // mic
    public const string Chevron = "\uF2B1";     // chevron_right
    public const string Paste = "\uF2D6";       // clipboard_paste
    public const string History = "\uF486";     // history
    public const string Presenter = "\uF62C";   // presenter

    /// <summary>Font-glyph ImageSource for buttons (always the fluent setter, never a ctor arg).</summary>
    public static MauiControls.FontImageSource Glyph(string glyph, Color color, double size = 20)
        => new() { FontFamily = FontFamily, Glyph = glyph, Color = color, Size = size };
}
