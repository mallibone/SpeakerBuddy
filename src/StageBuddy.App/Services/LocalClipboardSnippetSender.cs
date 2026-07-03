using StageBuddy.Core.Sync;

namespace StageBuddy.App.Services;

public sealed class LocalClipboardSnippetSender : ISnippetSender
{
    public ConnectionState State => ConnectionState.Connected;

#pragma warning disable CS0067 // never raised: this sender's state never changes
    public event EventHandler<ConnectionState>? StateChanged;
#pragma warning restore CS0067

    public void StartPairing()
    {
    }

    public void StopPairing()
    {
    }

    public async Task<SendResult> SendAsync(Snippet snippet, CancellationToken ct = default)
    {
        await Clipboard.Default.SetTextAsync(snippet.Text);
        return SendResult.Sent;
    }
}
