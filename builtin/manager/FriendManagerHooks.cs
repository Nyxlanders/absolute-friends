using AbsoluteFriends.Hooks;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Manager;

internal class FriendManagerHooks : BaseHooks
{
    private static float _shortcutTime = float.NegativeInfinity;

    private static bool _shortcutPending;

    private static int _shortcutFrame = -1;

    protected override Configurable<bool>[] Options => [];

    protected override bool IsOptionEnabled => true;

    protected override string? Subject => "Friend Manager";

    internal static bool ConsumeShortcut()
    {
        bool pending = _shortcutPending && Time.unscaledTime - _shortcutTime <= 0.2f;

        _shortcutPending = false;

        return pending;
    }

    private static bool IsShortcutDown(KeyCode key)
    {
        using (InputBlocker.Allow())
        {
            return Input.GetKeyDown(key);
        }
    }

    private static void TryOpen(ProcessManager manager, RainWorldGame game)
    {
        if (manager.IsSwitchingProcesses())
        {
            _shortcutPending = false;

            return;
        }

        if (manager.IsRunningAnyDialog || !ConsumeShortcut())
        {
            return;
        }

        FriendManagerDialog dialog = new(manager, game);

        manager.ShowDialog(dialog);

        if (ReferenceEquals(manager.dialog, dialog))
        {
            dialog.PauseGame();
        }
    }

    [HookPatch(typeof(On.RainWorld), nameof(On.RainWorld.Update))]
    private static void On_RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
    {
        KeyCode key = Config.ManagerKey.Value;

        if (key != KeyCode.None && Time.frameCount != _shortcutFrame && IsShortcutDown(key))
        {
            _shortcutFrame = Time.frameCount;
            _shortcutTime = Time.unscaledTime;
            _shortcutPending = true;
        }

        orig(self);

        if (self.processManager?.dialog is FriendManagerDialog dialog)
        {
            dialog.CollectScroll();
        }
    }

    [HookPatch(typeof(On.SaveState), nameof(On.SaveState.LoadGame))]
    private static void On_SaveState_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string saveString, RainWorldGame game)
    {
        orig(self, saveString, game);

        FriendSaveData.Reset();
    }

    [HookPatch(typeof(On.RainWorldGame), nameof(On.RainWorldGame.ctor))]
    private static void On_RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        orig(self, manager);

        FriendSaveData.Reset();
    }

    [HookPatch(typeof(On.Menu.PauseMenu), nameof(On.Menu.PauseMenu.Update))]
    private static void On_PauseMenu_Update(On.Menu.PauseMenu.orig_Update orig, PauseMenu self)
    {
        if (self.manager.dialog is FriendManagerDialog)
        {
            return;
        }

        orig(self);

        TryOpen(self.manager, self.game);
    }

    [HookPatch(typeof(On.Menu.Menu), nameof(On.Menu.Menu.PlaySound_SoundID))]
    private static void On_Menu_PlaySound_SoundID(On.Menu.Menu.orig_PlaySound_SoundID orig, global::Menu.Menu self, SoundID soundID)
    {
        if (self is not FriendManagerDialog)
        {
            orig(self, soundID);
        }
    }

    [HookPatch(typeof(On.Menu.Menu), nameof(On.Menu.Menu.PlaySound_SoundID_float_float_float))]
    private static void On_Menu_PlaySound_SoundID_float_float_float(On.Menu.Menu.orig_PlaySound_SoundID_float_float_float orig, global::Menu.Menu self, SoundID soundID, float pan, float vol, float pitch)
    {
        if (self is not FriendManagerDialog)
        {
            orig(self, soundID, pan, vol, pitch);
        }
    }

    [HookPatch(typeof(On.Menu.Remix.MixedUI.OpTab), nameof(On.Menu.Remix.MixedUI.OpTab._Update))]
    private static void On_OpTab_Update(On.Menu.Remix.MixedUI.OpTab.orig__Update orig, OpTab self)
    {
        if (self is not WrappedMenuTab { wrapper.menu: FriendManagerDialog dialog } || dialog.HeldElement is not { } held)
        {
            orig(self);

            return;
        }

        if (!held.IsInactive)
        {
            held.Update();
        }
    }

    [HookPatch(typeof(On.RainWorldGame), nameof(On.RainWorldGame.Update))]
    private static void On_RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        TryOpen(self.manager, self);

        orig(self);

        FriendTracking.MeetNewFriends(self);
    }
}
