using System.Reflection;
using AbsoluteFriends.Diagnostics;
using MonoMod.RuntimeDetour;
using RWCustom;
using UnityEngine;

namespace AbsoluteFriends.Manager;

internal static class InputBlocker
{
    private static readonly AddonLogger _logger = Reporter.GetLogger(Plugin.Name);

    private static readonly List<Hook> _hooks = [];

    private static int _passes;

    private static bool IsBlocking => _passes == 0 && Custom.rainWorld?.processManager?.dialog is FriendManagerDialog;

    internal static void Register()
    {
        if (_hooks.Count > 0)
        {
            return;
        }

        Add(nameof(Input.GetKey), typeof(KeyCode), new Func<Func<KeyCode, bool>, KeyCode, bool>(BlockKey));
        Add(nameof(Input.GetKey), typeof(string), new Func<Func<string, bool>, string, bool>(BlockKeyName));
        Add(nameof(Input.GetKeyDown), typeof(KeyCode), new Func<Func<KeyCode, bool>, KeyCode, bool>(BlockKey));
        Add(nameof(Input.GetKeyDown), typeof(string), new Func<Func<string, bool>, string, bool>(BlockKeyName));
        Add(nameof(Input.GetKeyUp), typeof(KeyCode), new Func<Func<KeyCode, bool>, KeyCode, bool>(BlockKey));
        Add(nameof(Input.GetKeyUp), typeof(string), new Func<Func<string, bool>, string, bool>(BlockKeyName));
        Add(nameof(Input.GetMouseButton), typeof(int), new Func<Func<int, bool>, int, bool>(BlockMouseButton));
        Add(nameof(Input.GetMouseButtonDown), typeof(int), new Func<Func<int, bool>, int, bool>(BlockMouseButton));
        Add(nameof(Input.GetMouseButtonUp), typeof(int), new Func<Func<int, bool>, int, bool>(BlockMouseButton));
    }

    internal static void Unregister()
    {
        _hooks.ForEach(hook => hook.Dispose());
        _hooks.Clear();
        _passes = 0;
    }

    internal static Pass Allow() => new();

    private static void Add(string method, Type parameter, Delegate blocker)
    {
        try
        {
            _hooks.Add(new Hook(typeof(Input).GetMethod(method, BindingFlags.Public | BindingFlags.Static, null, [parameter], null), blocker));
        }
        catch (Exception exception)
        {
            _logger.LogWarning($"Could not block Input.{method}({parameter.Name}) while the friend manager is open", exception);
        }
    }

    private static bool BlockKey(Func<KeyCode, bool> orig, KeyCode key) => !IsBlocking && orig(key);

    private static bool BlockKeyName(Func<string, bool> orig, string name) => !IsBlocking && orig(name);

    private static bool BlockMouseButton(Func<int, bool> orig, int button) => !IsBlocking && orig(button);

    internal readonly struct Pass : IDisposable
    {
        public Pass() => _passes++;

        public void Dispose() => _passes--;
    }
}
