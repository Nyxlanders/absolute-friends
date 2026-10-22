using AbsoluteFriends.Profiles;
using AbsoluteFriends.Utils;
using Menu.Remix.MixedUI;

namespace AbsoluteFriends.Options;

internal partial class RemixMenu
{
    private static string ProfileTime(string? value)
    {
        return DateTimeOffset.TryParse(value, out DateTimeOffset time) ? time.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "";
    }

    private void BuildProfiles(OpTab tab)
    {
        BeginTab(tab);
        AddTitle("Profiles");
        AddLabel("Profiles save addon and option settings.");
        AddLabel("Selecting a profile loads it; applying saves the changes to the profile.");

        _profiles.Clear();
        _profiles.AddRange(ProfileStore.Load());
        _profileIndex = _profiles.FindIndex(profile => profile.Id == Config.ProfileSelection.Value);

        if (_profileIndex < 0 && _profiles.Count > 0)
        {
            _profileIndex = 0;
        }
        _profileFilter = AddListSearch("_searchProfiles", "Shows only the profiles whose name contains what is typed here.");
        _profileViews.Clear();

        BeginSection();
        AddContainer(delegate
        {
            SetColumns(1);
            _createEditor = AddProfileCreator("Create Profile", "Creates a new profile from the current option values.", "Profile", CreateProfile);
        });

        int profileSlots = Math.Max(_profiles.Count + SpareProfileSlots, MinimumProfileSlots);

        _profileRadioGroup = AddRadioButtonGroup(CreateProfileSelection());
        _profileRadioGroup?.OnValueUpdate += (_, value, _) =>
        {
            if (int.TryParse(value, out int slot))
            {
                SelectProfile(slot);
            }
        };

        for (int slot = 0; slot < profileSlots; slot++)
        {
            _profileViews.Add(BuildProfileView(slot));
        }

        Watch(RefreshProfileViews);
        RefreshProfileViews();
    }

    private Configurable<int> CreateProfileSelection()
    {
        return CreateTransientConfigurable(
            Math.Max(_profileIndex, 0),
            new ConfigurableInfo(Translate("Selects this profile."))
        );
    }

    private ProfileView BuildProfileView(int slot)
    {
        OpRect? container = null;

        BeginSection(gate: () => ProfileMatches(slot));
        BeginBox();

        SetColumns(1);
        AddRadioButton("", out OpLabel? name, "Selects this profile.");

        ProfileEditor editor = AddProfileEditor(
            name!,
            "Edits this profile's name.",
            "",
            value => RenameProfile(slot, value)
        );

        SetColumns(17);

        OpLabel addons = AddNote("Addons enabled: <PLACEHOLDER>", "", alignment: FLabelAlignment.Left, span: 8f)!;
        OpLabel options = AddNote("Options enabled: <PLACEHOLDER>", "", alignment: FLabelAlignment.Right, span: 8f)!;

        AddFoldButton(
            "Shows or hides this profile's details.",
            () => IsProfileExpanded(slot),
            () => ToggleProfile(slot, container)
        );

        BeginFold(() => IsProfileExpanded(slot));

        SetColumns(2);

        OpLabel created = AddNote("Created: <PLACEHOLDER>", "", alignment: FLabelAlignment.Left)!;
        OpLabel modified = AddNote("Modified: <PLACEHOLDER>", "", alignment: FLabelAlignment.Right)!;

        SetColumns(1);

        OpLabel unavailable = AddNote("Unavailable: <PLACEHOLDER> addons and <PLACEHOLDER> options", alignment: FLabelAlignment.Left)!;

        SetColumns(10);
        AddColumn(8f);
        AddHoldButton("Remove Profile", "Hold to remove this profile.", () => RemoveProfile(slot), span: 2f);

        container = EndBox();

        return new(editor, created, modified, addons, options, unavailable);
    }

    private bool IsProfileExpanded(int slot)
    {
        return ProfileAt(slot) is { } profile && _expandedProfiles.Contains(profile.Id);
    }

    private void ToggleProfile(int slot, UIelement? anchor)
    {
        if (ProfileAt(slot) is not { } profile)
        {
            return;
        }

        if (!_expandedProfiles.Remove(profile.Id))
        {
            _expandedProfiles.Add(profile.Id);
        }

        _profileFilter?.Focus(anchor);
    }

    private Profile? ProfileAt(int slot) => slot >= 0 && slot < _profiles.Count ? _profiles[slot] : null;

    private bool ProfileMatches(int slot)
    {
        if (ProfileAt(slot) is not { } profile)
        {
            return false;
        }

        string query = _profileFilter?.Query?.Trim() ?? "";

        return query.Length == 0 || profile.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void RefreshProfileViews()
    {
        for (int slot = 0; slot < _profileViews.Count; slot++)
        {
            ProfileView view = _profileViews[slot];
            Profile? profile = ProfileAt(slot);

            view.Editor.Refresh(profile?.Name ?? "");
            view.Created.text = Translate("Created: <PLACEHOLDER>").FillPlaceholders(ProfileTime(profile?.CreatedAt));
            view.Modified.text = Translate("Modified: <PLACEHOLDER>").FillPlaceholders(ProfileTime(profile?.ModifiedAt));

            ProfileSummary summary = profile == null ? default : ProfileManager.Summarize(profile);

            view.Addons.text = Translate("Addons enabled: <PLACEHOLDER>").FillPlaceholders($"{summary.EnabledAddons} / {summary.Addons}");
            view.Options.text = Translate("Options enabled: <PLACEHOLDER>").FillPlaceholders($"{summary.EnabledOptions} / {summary.Options}");
            view.Unavailable.text = Translate("Unavailable: <PLACEHOLDER> addons and <PLACEHOLDER> options").FillPlaceholders(
                summary.UnavailableAddons,
                summary.UnavailableOptions
            );
        }

        if (_profileSelector != null)
        {
            RefreshProfileSelector();
        }

        if (_profileRadioGroup != null && _profileIndex >= 0 && _profileRadioGroup.value != _profileIndex.ToString())
        {
            _profileRadioGroup.value = _profileIndex.ToString();
        }

        _profileFilter?.Refresh();
    }

    private void RefreshProfileEditorVisibility()
    {
        _createEditor?.RefreshVisibility(true);

        for (int slot = 0; slot < _profileViews.Count; slot++)
        {
            _profileViews[slot].Editor.RefreshVisibility(ProfileMatches(slot));
        }
    }

    private ListItem[] ProfileChoices()
    {
        ListItem[] profiles = [.. _profiles.Select((profile, index) => new ListItem(profile.Id, profile.Name, index))];

        return _profiles.Count == 0
            ? [new ListItem(Config.NoProfile, Translate("No profiles saved"))]
            : profiles;
    }

    private void RefreshProfileSelector()
    {
        if (_profileSelector is not { } selector)
        {
            return;
        }

        ListItem[] choices = ProfileChoices();
        string choiceState = _profiles.Count == 0
            ? Config.NoProfile
            : string.Join("\n", _profiles.Select(profile => $"{profile.Id}\0{profile.Name}"));

        if (_profileChoices != choiceState)
        {
            _profileChoices = choiceState;

            string[] existing = [.. selector.GetItemList().Select(item => item.name)];

            selector.AddItems(sort: false, choices);
            selector.RemoveItems(selectNext: false, existing);
        }

        string selected = CurrentProfile?.Id ?? Config.NoProfile;

        if (selector.value != selected)
        {
            selector.value = selected;
        }
    }

    private class ProfileView(ProfileEditor editor, OpLabel created, OpLabel modified, OpLabel addons, OpLabel options, OpLabel unavailable)
    {
        internal ProfileEditor Editor { get; } = editor;

        internal OpLabel Created { get; } = created;

        internal OpLabel Modified { get; } = modified;

        internal OpLabel Addons { get; } = addons;

        internal OpLabel Options { get; } = options;

        internal OpLabel Unavailable { get; } = unavailable;
    }
}
