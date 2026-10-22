using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Profiles;
using AbsoluteFriends.Utils;

namespace AbsoluteFriends.Options;

internal partial class RemixMenu
{
    internal void KeepSelectedProfile()
    {
        if (_profiles.Find(profile => profile.Id == Config.ProfileSelection.Value) is not { } profile)
        {
            return;
        }

        try
        {
            if (ProfileManager.Keep(profile))
            {
                RefreshProfileViews();
            }
        }
        catch (Exception exception)
        {
            ProfileFailure("Could not update profile.", exception);
        }
    }

    private void SelectProfile(int slot) => SelectProfile(slot, reapplyCurrent: true);

    private void SelectProfile(string id) => SelectProfile(_profiles.FindIndex(profile => profile.Id == id), reapplyCurrent: false);

    private void SelectProfile(int slot, bool reapplyCurrent)
    {
        if (ProfileAt(slot) == null || (!reapplyCurrent && slot == _profileIndex))
        {
            return;
        }

        _profileIndex = slot;

        ApplyProfile();
        RefreshProfileViews();
    }

    private void CreateProfile(string name)
    {
        try
        {
            var created = ProfileManager.Create(name);

            _profiles.Insert(0, created.Profile);
            _profileIndex = 0;

            ShowProfileResult(created.Profile, "Profile created.");
            RefreshProfileViews();
            _profileFilter?.ScrollToTop();
        }
        catch (Exception exception)
        {
            ProfileFailure("Could not create profile.", exception);
        }
    }

    private void ApplyProfile()
    {
        if (CurrentProfile is not { } profile)
        {
            return;
        }

        ProfileManager.Apply(profile);
        ShowProfileResult(profile, "Profile loaded.");
    }

    private void RenameProfile(int index, string name)
    {
        if (ProfileAt(index) is not { } profile)
        {
            return;
        }

        try
        {
            ProfileManager.Rename(profile, name);

            RefreshProfileViews();
            SetProfileStatus("Profile renamed.");
        }
        catch (Exception exception)
        {
            ProfileFailure("Could not rename profile.", exception);
        }
    }

    private void RemoveProfile(int index)
    {
        if (ProfileAt(index) is not { } profile)
        {
            return;
        }

        try
        {
            bool selected = index == _profileIndex;

            ProfileManager.Remove(profile);
            _profiles.RemoveAt(index);

            if (_profileIndex > index)
            {
                _profileIndex--;
            }
            else if (_profileIndex == index)
            {
                _profileIndex = Math.Min(index, _profiles.Count - 1);
            }

            if (_profileIndex >= _profiles.Count)
            {
                _profileIndex = _profiles.Count - 1;
            }

            if (selected)
            {
                ApplyProfile();
            }

            RefreshProfileViews();
            SetProfileStatus("Profile removed.");
        }
        catch (Exception exception)
        {
            ProfileFailure("Could not remove profile.", exception);
        }
    }

    private void ProfileFailure(string message, Exception exception)
    {
        Reporter.LogError(message, exception);
        SetProfileStatus(message);
    }

    private void SetProfileStatus(string text)
    {
        Menu.Remix.ConfigConnector.ShowAlert(Translate(text));
    }

    private void ShowProfileResult(Profile profile, string action)
    {
        ProfileSummary summary = ProfileManager.Summarize(profile);
        string status = Translate("<PLACEHOLDER> Enabled: <PLACEHOLDER> of <PLACEHOLDER> addons and <PLACEHOLDER> of <PLACEHOLDER> options.").FillPlaceholders(
            Translate(action),
            summary.EnabledAddons,
            summary.Addons,
            summary.EnabledOptions,
            summary.Options
        );

        if (summary.UnavailableAddons > 0 || summary.UnavailableOptions > 0)
        {
            status += " " + Translate("Unavailable: <PLACEHOLDER> addons and <PLACEHOLDER> options").FillPlaceholders(summary.UnavailableAddons, summary.UnavailableOptions);
        }

        SetProfileStatus(status);
    }
}
