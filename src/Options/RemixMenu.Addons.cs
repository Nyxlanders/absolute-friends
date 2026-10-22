using AbsoluteFriends.Addons;
using AbsoluteFriends.Utils;
using Menu.Remix.MixedUI;

namespace AbsoluteFriends.Options;

internal partial class RemixMenu
{
    private void BuildAddonGroups(OpTab tab)
    {
        BeginTab(tab);
        AddTitle("Addons");
        AddLabel("Select a profile, then enable addons and configure their options.");

        SetColumns(6);
        _profileSelector = AddComboBox(Config.ProfileSelection, ProfileChoices(), "Profile", span: 6f);

        if (_profileSelector != null)
        {
            _profileSelector.OnValueUpdate += (_, value, _) => SelectProfile(value);
            RefreshProfileSelector();
        }

        OpRadioButtonGroup? view = AddRadioButtonRow(
            Config.AddonView,
            "View",
            ("Simple", "Shows switches that turn whole features on or off at once."),
            ("Detailed", "Shows every option of each addon.")
        );
        SectionFilter? filter = AddSearchBox("_searchAddons", "Search", "Shows only the addons whose name or options contain what is typed here.", span: 6f);

        PinHeader();

        view?.OnValueUpdate += (_, _, _) => filter?.ScrollToTop();

        Addon[] addons = [.. AddonRegistry.Ordered.Where(addon => !addon.IsApplied || addon.HasMenu)];

        Watch(() => Search(filter, addons));

        BuildAddonGroup("Built-In Addons", [.. addons.Where(addon => addon.IsBuiltIn)], filter);
        BuildAddonGroup("External Addons", [.. addons.Where(addon => !addon.IsBuiltIn)], filter);
    }

    private void BuildAddonGroup(string title, Addon[] addons, SectionFilter? filter)
    {
        if (addons.Length == 0)
        {
            return;
        }

        BeginSection([.. addons.SelectMany(addon => addon.Keywords)]);
        AddTitle(title);

        foreach (var addon in addons)
        {
            BuildAddon(addon, filter);
        }
    }

    private void BuildAddon(Addon addon, SectionFilter? filter)
    {
        OpRect? container = null;

        BeginSection([.. addon.Keywords], addon.IsApplied ? () => IsDetailed : null);
        BeginBox();

        SetColumns(1);
        if (addon.Enabled is { } enabled)
        {
            AddCheckBox(enabled, enabled: addon.AddonDependenciesApplied);
            AddDescription(addon.AddonDescription);
        }
        else
        {
            AddNote(addon.Name, alignment: FLabelAlignment.Left);
        }

        if (!addon.IsApplied && AddLabel("Remix is not enabled.", alignment: FLabelAlignment.Left) is { } notApplied)
        {
            notApplied.color = Palette.Warning;
        }

        if (addon.IsMismatched && AddLabel("Built for Absolute Friends v<PLACEHOLDER>; it may not work correctly.", addon.AbsoluteFriendsVersion, alignment: FLabelAlignment.Left) is { } warning)
        {
            warning.color = Palette.Warning;
        }

        if (!addon.IsApplied)
        {
            EndBox();

            return;
        }

        SetColumns(16);
        AddColumn(8f);

        OpLabel? options = AddNote("Options enabled: <PLACEHOLDER>", OptionCountText(addon), span: 7f);

        AddFoldButton(
            "Shows or hides this addon's options.",
            () => IsExpanded(addon),
            () => Expand(addon, filter, container),
            span: 1f
        );

        if (options != null)
        {
            string template = Translate("Options enabled: <PLACEHOLDER>");

            Watch(() => Retitle(options, template, addon));
        }

        BeginFold(() => IsExpanded(addon));

        SetColumns(4);
        WithButtonRequirement(addon.Enabled, () => AddonRegistry.BuildMenu(addon, this));

        container = EndBox();

        BuildSimpleAddon(addon);
    }

    private bool IsExpanded(Addon addon)
    {
        return _expanded.Contains(addon) || _searched.Contains(addon);
    }

    private void Expand(Addon addon, SectionFilter? filter, UIelement? anchor)
    {
        if (_searched.Remove(addon) | _expanded.Remove(addon))
        {
            return;
        }

        _expanded.Add(addon);

        filter?.Focus(anchor);
    }

    private void Search(SectionFilter? filter, Addon[] addons)
    {
        if (filter?.Query is not { } query || query == _query)
        {
            return;
        }

        _query = query;

        _searched.Clear();

        if (query.Length == 0)
        {
            return;
        }

        foreach (var addon in addons.Where(addon => addon.HasOption(query)))
        {
            _searched.Add(addon);
        }
    }
}
