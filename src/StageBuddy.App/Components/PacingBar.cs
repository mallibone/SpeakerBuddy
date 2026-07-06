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

        var paceColor = _snapshot.Pace switch
        {
            PaceStatus.OnPaceOrAhead => ApplicationTheme.PaceOnTrack,
            PaceStatus.WithinTolerance => ApplicationTheme.PaceTolerance,
            _ => ApplicationTheme.PaceBehind
        };

        return VStack(
            ProgressBar()
                .Progress(_snapshot.OverallProgress)
                .ProgressColor(paceColor)
                .ScaleY(2.5),

            // Grid, not HStack: HStack has no per-child expansion, so nothing can push
            // the remaining-time label to the trailing edge.
            Grid("auto", "*,*",
                Label($"Elapsed {FormatTime(_snapshot.Elapsed)}")
                    .FontFamily("OpenSansSemibold")
                    .TextColor(ApplicationTheme.TextSecondary)
                    .GridColumn(0),
                Label($"Remaining {FormatTime(_snapshot.Remaining)}")
                    .FontFamily("OpenSansSemibold")
                    .TextColor(paceColor)
                    .HorizontalTextAlignment(TextAlignment.End)
                    .GridColumn(1)
            )
        )
        .Spacing(10);
    }

    private static string FormatTime(TimeSpan span) => span.ToString(@"mm\:ss");
}
