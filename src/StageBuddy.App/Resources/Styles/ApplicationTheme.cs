using MauiReactor;
using MauiReactor.Shapes;

namespace StageBuddy.App.Resources.Styles;

/// <summary>
/// Stage Buddy "stage dark" theme: deep-navy surfaces with a warm amber spotlight accent.
/// Components must use the semantic tokens (Accent, Pace*, Card*, Code*) — never raw Colors.*.
/// </summary>
class ApplicationTheme : Theme
{
    // ---- Brand palette ----
    public static Color Navy950 { get; } = Color.FromArgb("#0E1220");
    public static Color Navy900 { get; } = Color.FromArgb("#10131F");   // dark surface
    public static Color Navy800 { get; } = Color.FromArgb("#1A2035");   // dark card
    public static Color Navy600 { get; } = Color.FromArgb("#39415A");   // dark strokes / tracks
    public static Color Amber300 { get; } = Color.FromArgb("#FFD489");
    public static Color Amber400 { get; } = Color.FromArgb("#FFB84D");  // dark-mode accent
    public static Color Amber600 { get; } = Color.FromArgb("#C97A08");  // light-mode accent
    public static Color AmberInk { get; } = Color.FromArgb("#2B1D04");  // text on amber

    public static Color White { get; } = Colors.White;
    public static Color Black { get; } = Colors.Black;

    public static Color Gray100 { get; } = Color.FromArgb("#E1E4EE");
    public static Color Gray200 { get; } = Color.FromArgb("#C8CCD9");
    public static Color Gray300 { get; } = Color.FromArgb("#ACACAC");
    public static Color Gray400 { get; } = Color.FromArgb("#919191");
    public static Color Gray500 { get; } = Color.FromArgb("#6E7486");
    public static Color Gray600 { get; } = Color.FromArgb("#404656");
    public static Color Gray900 { get; } = Color.FromArgb("#212430");

    // ---- Semantic tokens (theme-aware) ----
    private static bool IsLight => MauiControls.Application.Current?.RequestedTheme != AppTheme.Dark;

    /// <summary>Primary interactive color — the amber spotlight.</summary>
    public static Color Accent => IsLight ? Amber600 : Amber400;
    public static Color OnAccent => IsLight ? White : AmberInk;

    /// <summary>Pace status colors (spec §5): green on/ahead, amber within tolerance, red behind.</summary>
    public static Color PaceOnTrack => IsLight ? Color.FromArgb("#1E9E52") : Color.FromArgb("#37D97F");
    public static Color PaceTolerance => IsLight ? Amber600 : Amber400;
    public static Color PaceBehind => IsLight ? Color.FromArgb("#D93025") : Color.FromArgb("#FF6B6B");

    public static Color Surface => IsLight ? White : Navy900;
    public static Color Card => IsLight ? Color.FromArgb("#F4F5F9") : Navy800;
    public static Color CardBorder => IsLight ? Gray100 : Navy600;
    public static Color TextPrimary => IsLight ? Gray900 : White;
    public static Color TextSecondary => IsLight ? Gray500 : Color.FromArgb("#9AA3B8");

    /// <summary>Code blocks stay dark in both themes — snippets read like an editor.</summary>
    public static Color CodeBackground { get; } = Color.FromArgb("#141926");
    public static Color CodeForeground { get; } = Color.FromArgb("#E6EAF2");

    protected override void OnApply()
    {
        ActivityIndicatorStyles.Default = _ =>
            _.Color(Accent);

        BorderStyles.Default = _ => _
            .Stroke(CardBorder)
            .StrokeShape(new RoundRectangle().CornerRadius(12))
            .StrokeThickness(1);

        BoxViewStyles.Default = _ => _
            .BackgroundColor(IsLight ? Gray900 : Gray200);

        ButtonStyles.Default = _ => _
            .TextColor(OnAccent)
            .BackgroundColor(Accent)
            .FontFamily("OpenSansSemibold")
            .FontSize(15)
            .BorderWidth(0)
            .CornerRadius(10)
            .Padding(14, 10)
            .MinimumHeightRequest(44)
            .MinimumWidthRequest(44)
            .VisualState("CommonStates", "Disable", MauiControls.Button.TextColorProperty, IsLight ? Gray400 : Gray500)
            .VisualState("CommonStates", "Disable", MauiControls.Button.BackgroundColorProperty, IsLight ? Gray100 : Gray600);

        // Quiet secondary buttons (toggles, disclosures) — opt in via .ThemeKey("Secondary").
        ButtonStyles.Themes["Secondary"] = _ => _
            .TextColor(Accent)
            .BackgroundColor(IsLight ? Color.FromArgb("#F4F5F9") : Navy800)
            .FontFamily("OpenSansSemibold")
            .FontSize(15)
            .BorderColor(CardBorder)
            .BorderWidth(1)
            .CornerRadius(10)
            .Padding(14, 10)
            .MinimumHeightRequest(44)
            .MinimumWidthRequest(44);

        EditorStyles.Default = _ => _
            .TextColor(TextPrimary)
            .BackgroundColor(Colors.Transparent)
            .FontFamily("OpenSansRegular")
            .FontSize(14)
            .PlaceholderColor(TextSecondary)
            .MinimumHeightRequest(44)
            .MinimumWidthRequest(44)
            .VisualState("CommonStates", "Disable", MauiControls.Editor.TextColorProperty, IsLight ? Gray300 : Gray600);

        EntryStyles.Default = _ => _
            .TextColor(TextPrimary)
            .BackgroundColor(Colors.Transparent)
            .FontFamily("OpenSansRegular")
            .FontSize(14)
            .PlaceholderColor(TextSecondary)
            .MinimumHeightRequest(44)
            .MinimumWidthRequest(44)
            .VisualState("CommonStates", "Disable", MauiControls.Entry.TextColorProperty, IsLight ? Gray300 : Gray600);

        ImageButtonStyles.Default = _ => _
            .Opacity(1)
            .BorderColor(Colors.Transparent)
            .BorderWidth(0)
            .CornerRadius(0)
            .MinimumHeightRequest(44)
            .MinimumWidthRequest(44)
            .VisualState("CommonStates", "Disable", MauiControls.ImageButton.OpacityProperty, 0.5);

        LabelStyles.Default = _ => _
            .TextColor(TextPrimary)
            .BackgroundColor(Colors.Transparent)
            .FontFamily("OpenSansRegular")
            .FontSize(14)
            .VisualState("CommonStates", "Disable", MauiControls.Label.TextColorProperty, IsLight ? Gray300 : Gray600);

        LabelStyles.Themes["Headline"] = _ => _
            .TextColor(TextPrimary)
            .FontFamily("OpenSansSemibold")
            .FontSize(28)
            .HorizontalOptions(MauiControls.LayoutOptions.Center)
            .HorizontalTextAlignment(TextAlignment.Center);

        LabelStyles.Themes["SubHeadline"] = _ => _
            .TextColor(TextSecondary)
            .FontSize(16)
            .HorizontalOptions(MauiControls.LayoutOptions.Center)
            .HorizontalTextAlignment(TextAlignment.Center);

        ProgressBarStyles.Default = _ => _
            .ProgressColor(Accent)
            .VisualState("CommonStates", "Disable", MauiControls.ProgressBar.ProgressColorProperty, IsLight ? Gray300 : Gray600);

        ShadowStyles.Default = _ => _
            .Radius(15)
            .Opacity(0.5f)
            .Brush(Black)
            .Offset(new Point(0, 4));

        SwitchStyles.Default = _ => _
            .OnColor(Accent)
            .ThumbColor(White)
            .VisualState("CommonStates", "Disable", MauiControls.Switch.OnColorProperty, IsLight ? Gray300 : Gray600)
            .VisualState("CommonStates", "Disable", MauiControls.Switch.ThumbColorProperty, IsLight ? Gray300 : Gray600);

        PageStyles.Default = _ => _
            .Padding(0)
            .BackgroundColor(Surface);

        NavigationPageStyles.Default = _ => _
            .Set(MauiControls.NavigationPage.BarBackgroundColorProperty, Surface)
            .Set(MauiControls.NavigationPage.BarTextColorProperty, TextPrimary)
            .Set(MauiControls.NavigationPage.IconColorProperty, Accent);
    }
}
