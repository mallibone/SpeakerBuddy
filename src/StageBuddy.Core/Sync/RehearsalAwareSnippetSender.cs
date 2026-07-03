namespace StageBuddy.Core.Sync;

public sealed class RehearsalAwareSnippetSender : ISnippetSender, IDisposable
{
    private readonly ISnippetSender _liveSender;
    private readonly ISnippetSender _rehearsalSender;
    private readonly IRehearsalModeToggle _toggle;

    public RehearsalAwareSnippetSender(ISnippetSender liveSender, ISnippetSender rehearsalSender, IRehearsalModeToggle toggle)
    {
        _liveSender = liveSender;
        _rehearsalSender = rehearsalSender;
        _toggle = toggle;

        _liveSender.StateChanged += OnLiveStateChanged;
        _rehearsalSender.StateChanged += OnRehearsalStateChanged;
        _toggle.Changed += OnToggleChanged;
    }

    public ISnippetSender LiveSender => _liveSender;

    private ISnippetSender Active => _toggle.IsRehearsalMode ? _rehearsalSender : _liveSender;

    public ConnectionState State => Active.State;

    public event EventHandler<ConnectionState>? StateChanged;

    public void StartPairing() => _liveSender.StartPairing();

    public void StopPairing() => _liveSender.StopPairing();

    public Task<SendResult> SendAsync(Snippet snippet, CancellationToken ct = default) => Active.SendAsync(snippet, ct);

    public void Dispose()
    {
        _liveSender.StateChanged -= OnLiveStateChanged;
        _rehearsalSender.StateChanged -= OnRehearsalStateChanged;
        _toggle.Changed -= OnToggleChanged;
    }

    private void OnLiveStateChanged(object? sender, ConnectionState state)
    {
        if (!_toggle.IsRehearsalMode)
            StateChanged?.Invoke(this, state);
    }

    private void OnRehearsalStateChanged(object? sender, ConnectionState state)
    {
        if (_toggle.IsRehearsalMode)
            StateChanged?.Invoke(this, state);
    }

    private void OnToggleChanged(object? sender, bool isRehearsalMode) => StateChanged?.Invoke(this, Active.State);
}
