using AbsoluteFriends.Diagnostics;
using Menu.Remix.MixedUI;

namespace AbsoluteFriends.Options;

internal partial class RemixMenu
{
    private void BuildDiagnostics(OpTab tab)
    {
        BeginTab(tab);
        AddTitle("Diagnostics");
        AddLabel("Broken features disable themselves and mark their options.");
        AddLabel("Enable debugging, reproduce the problem, then send <PLACEHOLDER>.", Reporter.ReportFileName);

        AddTitle("Report");
        SetColumns(2);
        AddCheckBox(Config.Debug);
        AddCheckBox(Config.HookBaselines);
        AddSimpleButton("Open Report", "Shows the saved report in the file browser.", Reporter.OpenReport, span: 2f);
    }
}
