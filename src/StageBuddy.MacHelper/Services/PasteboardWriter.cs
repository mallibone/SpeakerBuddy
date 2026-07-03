namespace StageBuddy.MacHelper.Services;

public static class PasteboardWriter
{
    public static Task WriteAsync(string text) => Clipboard.Default.SetTextAsync(text);
}
