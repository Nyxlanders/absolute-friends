using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Manager;

public class FriendTab
{
    private readonly FriendManagerDialog _dialog;

    private readonly MenuTabWrapper _wrapper;

    private readonly List<UIelement> _elements = [];

    private readonly List<UIelement> _hiddenByTab = [];

    internal FriendTab(FriendManagerDialog dialog, MenuTabWrapper wrapper, Vector2 origin, Vector2 size, FriendManager.TabRegistration registration, bool builtIn)
    {
        _dialog = dialog;
        _wrapper = wrapper;
        Origin = origin;
        Registration = registration;
        BuiltIn = builtIn;
        Size = size;
        Container = new()
        {
            x = origin.x + 0.01f,
            y = origin.y + 0.01f,
            isVisible = false
        };

        wrapper._tab.myContainer.AddChild(Container);
    }

    public event Action<AbstractCreature?>? FriendChanged;

    public event Action? Refresh;

    public event Action? Update;

    public event Action<float>? GrafUpdate;

    public event Action? Shown;

    public event Action? Hidden;

    public event Action? Closed;

    public Vector2 Size { get; }

    public Vector2 Origin { get; }

    public FContainer Container { get; }

    public AbstractCreature? Friend { get; private set; }

    public bool IsActive { get; private set; }

    internal FriendManager.TabRegistration Registration { get; }

    internal bool BuiltIn { get; }

    internal bool Visible { get; set; }

    internal bool Failed { get; set; }

    internal IEnumerable<UIelement> Elements => _elements;

    public void Add(UIelement element)
    {
        if (element == null || _elements.Contains(element))
        {
            return;
        }

        if (!element.InScrollBox)
        {
            element.SetPos(element.GetPos() + Origin);
        }

        _ = new UIelementWrapper(_wrapper, element);
        _elements.Add(element);

        if (!IsActive && !element.Hidden)
        {
            element.Hide();
            _hiddenByTab.Add(element);
        }
    }

    public void Add(OpScrollBox scrollBox, UIelement element)
    {
        if (element == null || _elements.Contains(element))
        {
            return;
        }

        scrollBox.AddItems(element);
        Add(element);
    }

    public void Remove(UIelement element)
    {
        if (!_elements.Remove(element))
        {
            return;
        }

        _hiddenByTab.Remove(element);

        if (_dialog.WrapperOf(element) is { } wrapper)
        {
            _dialog.RemoveElement(wrapper);
        }
    }

    public void RefreshManager() => _dialog.RefreshManager();

    internal void SetActive(bool active)
    {
        if (IsActive == active)
        {
            return;
        }

        IsActive = active;
        Container.isVisible = active;

        if (active)
        {
            foreach (var element in _hiddenByTab)
            {
                element.Show();
                FriendManagerDialog.Snap(element);
            }

            _hiddenByTab.Clear();
            Shown?.Invoke();
        }
        else
        {
            foreach (var element in _elements.Where(element => !element.Hidden))
            {
                element.Hide();
                _hiddenByTab.Add(element);
            }

            Hidden?.Invoke();
        }
    }

    internal void ChangeFriend(AbstractCreature? friend)
    {
        Friend = friend;
        FriendChanged?.Invoke(friend);
    }

    internal void RaiseRefresh() => Refresh?.Invoke();

    internal void RaiseUpdate() => Update?.Invoke();

    internal void RaiseGrafUpdate(float timeStacker) => GrafUpdate?.Invoke(timeStacker);

    internal void Close()
    {
        try
        {
            Closed?.Invoke();
        }
        finally
        {
            Container.RemoveFromContainer();
        }
    }
}
