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
            return Label("Snippet too long to show as a QR code")
                .TextColor(ApplicationTheme.PaceTolerance);

        return VStack(
            HStack(
                Label(Icons.Qr)
                    .FontFamily(Icons.FontFamily)
                    .FontSize(14)
                    .TextColor(ApplicationTheme.TextSecondary)
                    .VCenter(),
                Label("Mac not connected — scan to copy")
                    .FontSize(14)
                    .TextColor(ApplicationTheme.TextSecondary)
                    .VCenter()
                    .AutomationId("QrFallbackCaption")
            )
            .Spacing(6)
            .HCenter(),

            // White card behind the code keeps the quiet zone intact on the dark theme.
            Border(
                Image()
                    .Source(ImageSource.FromStream(() => new MemoryStream(bytes)))
                    .WidthRequest(220)
                    .HeightRequest(220)
                    .AutomationId("QrFallbackImage")
            )
            .BackgroundColor(Colors.White)
            .Padding(10)
            .HCenter()
        )
        .Spacing(10);
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
