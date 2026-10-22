using AbsoluteFriends.Addons;

namespace AbsoluteFriends.Options;

internal partial class RemixMenu
{
    private static SwitchState StateOf(Configurable<bool>[] options)
    {
        int count = options.Count(option => option.IsChecked);

        return count == 0 ? SwitchState.Off : count == options.Length ? SwitchState.On : SwitchState.Mixed;
    }

    private static void Toggle(Configurable<bool>[] options)
    {
        bool value = StateOf(options) != SwitchState.On;

        foreach (var option in options)
        {
            option.Check(value);
        }
    }

    private static bool IsAllowed(Configurable<bool>[] options, Configurable<bool> option)
    {
        return option.Requirements.All(masters => masters.Any(master => IsSatisfied(options, master)));
    }

    private static bool IsSatisfied(Configurable<bool>[] options, Configurable<bool> master)
    {
        return options.Contains(master) || (master.IsChecked && IsAllowed(options, master));
    }

    private bool IsAvailable(Addon addon, Configurable<bool>[] options)
    {
        return (addon.Enabled == null || (IsUsable(addon.Enabled) && addon.Enabled.IsChecked)) && options.Any(option => IsAllowed(options, option));
    }

    private void BuildSimpleAddon(Addon addon)
    {
        int switches = 0;

        BeginSection([.. addon.Keywords, .. addon.Groups.Select(group => group.Label)], () => !IsDetailed);
        BeginBox();

        SetColumns(3);
        if (addon.Enabled is { } enabled)
        {
            AddSwitch(
                addon.Name,
                enabled.Description ?? "",
                () => enabled.IsChecked ? SwitchState.On : SwitchState.Off,
                () => IsUsable(enabled),
                () => enabled.Check(!enabled.IsChecked),
                span: 3f,
                meadowNote: MeadowNoteOf([enabled])
            );
            AddDescription(addon.AddonDescription);
        }
        else
        {
            AddNote(addon.Name, alignment: FLabelAlignment.Left, span: 3f);
        }

        foreach (var group in addon.Groups)
        {
            Configurable<bool>[] options = [.. group.Options.Where(HasControl)];

            if (options.Length == 0)
            {
                continue;
            }

            AddSwitch(group.Label, group.Description, () => StateOf(options), () => IsAvailable(addon, options), () => Toggle(options), meadowNote: MeadowNoteOf(options));
            switches++;
        }

        if (switches == 0 && addon.HasUngroupedOptions)
        {
            SetColumns(1);
            AddNote("Options are in the Detailed view.", alignment: FLabelAlignment.Left);
        }

        EndBox();
    }
}
