using StageBuddy.Core.Live;

namespace StageBuddy.App.Components;

partial class PacingBar : Component
{
    [Prop]
    LiveTalkSnapshot? _snapshot;

    public override VisualNode Render()
    {
        if (_snapshot is null)
            return VStack();

        var color = _snapshot.Pace switch
        {
            PaceStatus.OnPaceOrAhead => Colors.Green,
            PaceStatus.WithinTolerance => Colors.Orange,
            _ => Colors.Red
        };

        return VStack(
            ProgressBar()
                .Progress(_snapshot.OverallProgress)
                .ProgressColor(color),

            HStack(
                Label($"Elapsed {FormatTime(_snapshot.Elapsed)}"),
                Label($"Remaining {FormatTime(_snapshot.Remaining)}")
            )
            .Spacing(24)
        )
        .Spacing(4);
    }

    private static string FormatTime(TimeSpan span) => span.ToString(@"mm\:ss");
}
