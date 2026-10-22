using AbsoluteFriends.Addons;
using AbsoluteFriends.Profiles;
using AbsoluteFriends.Utils;
using Menu.Remix.MixedUI;

namespace AbsoluteFriends.Options;

internal partial class RemixMenu : MenuBuilder
{
    private const int MinimumProfileSlots = 32;

    private const int SpareProfileSlots = 16;

    public static readonly RemixMenu Instance = new();

    private readonly HashSet<Addon> _expanded = [];

    private readonly HashSet<string> _expandedProfiles = [];

    private readonly HashSet<Addon> _searched = [];

    private readonly List<Profile> _profiles = [];

    private readonly List<ProfileView> _profileViews = [];

    private ProfileEditor? _createEditor;

    private OpComboBox? _profileSelector;

    private OpRadioButtonGroup? _profileRadioGroup;

    private SectionFilter? _profileFilter;

    private int _profileIndex = -1;

    private string _profileChoices = "";

    private string _query = "";

    private RemixMenu() => Config.Bind(this);

    private Profile? CurrentProfile => _profileIndex >= 0 && _profileIndex < _profiles.Count ? _profiles[_profileIndex] : null;

    private static bool IsDetailed => Config.AddonView.BoundUIconfig is { } control && int.TryParse(control.value, out int value) ? value == 1 : Config.AddonView.Value == 1;

    public override void Initialize()
    {
        base.Initialize();

        _profileSelector = null;
        _profileRadioGroup = null;
        _profileFilter = null;
        _createEditor = null;
        _profileChoices = "";

        OpTab addonTab = new(this, Translate("Addons"));
        OpTab profileTab = new(this, Translate("Profiles"));
        OpTab diagnosticsTab = new(this, Translate("Diagnostics"));

        Tabs = [addonTab, profileTab, diagnosticsTab];

        BuildProfiles(profileTab);
        BuildAddonGroups(addonTab);
        BuildDiagnostics(diagnosticsTab);
        EndTab();

        BindRequirements();
        Watch(RefreshProfileEditorVisibility);
    }

    private static void Retitle(OpLabel label, string template, Addon addon)
    {
        string text = template.FillPlaceholders(OptionCountText(addon));

        if (label.text != text)
        {
            label.text = text;
        }
    }

    private static string OptionCountText(Addon addon)
    {
        return $"{addon.EnabledOptionCount} / {addon.OptionCount}";
    }

    private SectionFilter? AddListSearch(string key, string description)
    {
        SetColumns(6);

        SectionFilter? filter = AddSearchBox(key, "Search", description, span: 6f);

        PinHeader();

        return filter;
    }

    private OpSimpleImageButton? AddFoldButton(string description, Func<bool> unfolded, Action action, float span = 1f)
    {
        OpSimpleImageButton? button = AddSimpleImageButton("Menu_Symbol_Arrow", description, action, span: span, width: 24f);

        if (button != null)
        {
            Watch(() => button.sprite.rotation = unfolded() ? 0f : 180f);
        }

        return button;
    }
}
