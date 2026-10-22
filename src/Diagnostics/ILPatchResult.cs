namespace AbsoluteFriends.Diagnostics;

internal enum ILPatchStatus
{
    NotFound,
    Exact,
    Shifted,
    Rearranged,
    CountMismatch,
    AnchorMismatch,
    Untracked,
}

internal readonly struct ILPatchSite(int index, int inserted, string anchor)
{
    public readonly int Index = index;

    public readonly int Inserted = inserted;

    public readonly string Anchor = anchor;

    public bool Rewritten => Inserted == 0;
}

internal class ILPatchResult(string fullName, ILPatchStatus status, int shift, int unreachable, List<ILPatchSite> sites, List<string> details)
{
    public bool IsHealthy => unreachable == 0 && status is ILPatchStatus.Exact or ILPatchStatus.Shifted or ILPatchStatus.Untracked;

    public string Signature => $"{status}/{shift}/{unreachable}/{FormatSites()}";

    public void Report(bool shared = false)
    {
        if (unreachable > 0)
        {
            Reporter.LogError($"{fullName}: UNREACHABLE ({unreachable} inserted instruction(s) cannot be reached, so this patch is in place but never runs)");
        }

        string summary = $"{fullName}: {FormatStatus(shared)} {FormatSites()}";

        switch (status)
        {
            case ILPatchStatus.Rearranged:
            case ILPatchStatus.CountMismatch or ILPatchStatus.AnchorMismatch when shared:
                Reporter.LogWarning(summary);

                break;

            case ILPatchStatus.NotFound or ILPatchStatus.CountMismatch or ILPatchStatus.AnchorMismatch:
                Reporter.LogError(summary);

                break;

            default:
                Reporter.LogInfo(summary);

                break;
        }

        if (HookBaseline.Enabled)
        {
            string attribute = $"[HookTest([{string.Join(", ", sites.Select(site => site.Index))}], [{string.Join(", ", sites.Select(site => $"\"{site.Anchor}\""))}])]";

            Reporter.LogInfo($"{fullName}: {attribute}");

            HookBaseline.Record(fullName, attribute);
        }

        Reporter.LogDetail(details);
    }

    private string FormatStatus(bool shared) => status switch
    {
        ILPatchStatus.Exact => "EXACT",
        ILPatchStatus.Shifted => $"SHIFTED ({shift:+#;-#;0}; another mod most likely patched this method first, but every pattern matched the recorded instruction in the same order)",
        ILPatchStatus.Rearranged => "REARRANGED (the same number of patterns matched, but not at a uniform offset)",
        ILPatchStatus.CountMismatch when shared => "COUNT MISMATCH (this mod patches this method more than once, so the recorded count includes the other patches)",
        ILPatchStatus.CountMismatch => "COUNT MISMATCH (a pattern matched a different number of times than recorded)",
        ILPatchStatus.AnchorMismatch when shared => "ANCHOR MISMATCH (this mod patches this method more than once, so the recorded instruction is whichever patch landed here first)",
        ILPatchStatus.AnchorMismatch => "ANCHOR MISMATCH (the patch landed on different instructions than were recorded, so the game's code has changed underneath it)",
        ILPatchStatus.NotFound => "NOT FOUND (nothing was inserted and nothing was rewritten, so this hook currently does nothing)",
        _ => "UNTRACKED (this hook has no recorded indices to compare against)",
    };

    private string FormatSites() => $"sites=[{string.Join(", ", sites.Select(site => site.Rewritten ? $"{site.Index}*" : $"{site.Index}+{site.Inserted}"))}]";
}
