using QRCoder;

namespace StageBuddy.App.Components;

partial class QrFallbackView : Component
{
    [Prop]
    string? _snippetText;

    public override VisualNode Render()
    {
        if (string.IsNullOrEmpty(_snippetText))
            return VStack();

        var bytes = TryGenerateQrPng(_snippetText);
        if (bytes is null)
            return Label("Snippet too long to show as a QR code").TextColor(Colors.Orange);

        return VStack(
            Label("Mac not connected — scan to copy")
                .FontSize(14)
                .TextColor(Colors.Gray),

            Image()
                .Source(ImageSource.FromStream(() => new MemoryStream(bytes)))
                .WidthRequest(220)
                .HeightRequest(220)
                .HCenter()
        )
        .Spacing(8);
    }

    private static byte[]? TryGenerateQrPng(string text)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
            var qrCode = new PngByteQRCode(qrData);
            return qrCode.GetGraphic(10);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
