using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using Menu.Remix;
using UnityEngine;

namespace AbsoluteFriends.Manager;

internal partial class FriendManagerDialog
{
    private void BuildTabs(Vector2 canvasPos, Vector2 columnPos)
    {
        Vector2 origin = canvasPos + new Vector2(CanvasInset + MenuBuilder.Padding, CanvasInset);
        Vector2 size = new(CanvasSize - (CanvasInset + MenuBuilder.Padding) * 2f, CanvasSize - CanvasInset * 2f - TopPadding);
        FriendManager.TabRegistration details = new("details", "Profile", tab => _ = new DetailsTab(this, tab), null);

        foreach (var registration in FriendManager.Tabs.Prepend(details))
        {
            FriendTab tab = new(this, _wrapper, origin, size, registration, registration == details);

            _tabs.Add(tab);

            Run(tab, () => registration.Build(tab));
        }

        _tabColumnX = columnPos.x;
        _tabColumnTop = columnPos.y + TabAreaHeight;

        for (int index = 0; index < Mathf.Min(_tabs.Count, MaxVisibleTabs); index++)
        {
            TabButton button = new(this);

            _tabButtons.Add(button);
            _ = new UIelementWrapper(_wrapper, button);
        }

        _tabScrollUp = AddScrollButton(true);
        _tabScrollDown = AddScrollButton(false);
    }

    private TabScrollButton AddScrollButton(bool up)
    {
        TabScrollButton button = new(new Vector2(_tabColumnX, up ? _tabColumnTop : _tabColumnTop - TabAreaHeight - 20f), up)
        {
            description = Translation.Of(up ? "Show earlier tabs." : "Show later tabs.")
        };

        button.OnClick += delegate { ScrollTabs(up ? -1 : 1); };
        _ = new UIelementWrapper(_wrapper, button);

        return button;
    }

    internal void RefreshManager()
    {
        RefreshMembership();
        RefreshRows();
    }

    private void UpdateTabs()
    {
        AbstractCreature? friend = _selectedFriend;

        foreach (var tab in _tabs)
        {
            bool visible = tab.BuiltIn;

            if (!visible && friend != null)
            {
                Run(tab, () => visible = tab.Registration.IsVisibleFor(friend));
            }

            tab.Visible = visible;
            Run(tab, () => tab.ChangeFriend(friend));
        }

        if (_activeTab is not { Visible: true, Failed: false })
        {
            ShowTab(FirstShownTab());
        }

        LayoutTabs();
    }

    private FriendTab? FirstShownTab() => _tabs.FirstOrDefault(tab => tab.Visible && !tab.Failed);

    private void LayoutTabs()
    {
        _shownTabs.Clear();
        _shownTabs.AddRange(_tabs.Where(tab => tab.Visible && !tab.Failed));

        int slots = _shownTabs.Count < 2 ? 0 : Mathf.Min(_shownTabs.Count, MaxVisibleTabs);
        float height = Mathf.Min(120f, TabAreaHeight / Mathf.Max(1, slots));

        _tabTop = Mathf.Clamp(_tabTop, 0, Mathf.Max(0, _shownTabs.Count - slots));

        for (int index = 0; index < _tabButtons.Count; index++)
        {
            TabButton button = _tabButtons[index];

            if (index >= slots)
            {
                SetShown(button, false);

                continue;
            }

            FriendTab tab = _shownTabs[_tabTop + index];

            button.Represent(tab, Translation.Of(tab.Registration.Title), tab == _activeTab);
            button.Place(new Vector2(_tabColumnX, _tabColumnTop - height * (index + 1) + 3f), height, index == 0, index == slots - 1);
            SetShown(button, true);
        }

        if (_tabScrollUp == null || _tabScrollDown == null)
        {
            return;
        }

        bool scrolls = slots > 0 && _shownTabs.Count > slots;

        SetShown(_tabScrollUp, scrolls);
        SetShown(_tabScrollDown, scrolls);
        _tabScrollUp.greyedOut = _tabTop <= 0;
        _tabScrollDown.greyedOut = _tabTop >= _shownTabs.Count - slots;
    }

    private void ScrollTabs(int delta)
    {
        int slots = Mathf.Min(_shownTabs.Count, MaxVisibleTabs);
        int top = Mathf.Clamp(_tabTop + delta, 0, Mathf.Max(0, _shownTabs.Count - slots));

        if (top == _tabTop)
        {
            return;
        }

        _tabTop = top;
        LayoutTabs();
    }

    private void ShowTab(FriendTab? tab)
    {
        if (_activeTab == tab)
        {
            return;
        }

        FriendTab? previous = _activeTab;

        _activeTab = tab;

        if (previous != null)
        {
            Run(previous, () => previous.SetActive(false));
        }

        if (tab != null && !Run(tab, () => tab.SetActive(true)))
        {
            return;
        }

        LayoutTabs();
        RefreshTab();
    }

    private void RefreshTab()
    {
        if (_activeTab is { } tab)
        {
            Run(tab, tab.RaiseRefresh);
        }
    }

    private void RefreshAll()
    {
        RefreshRows();
        RefreshTab();
    }

    private void UpdateTab()
    {
        if (_activeTab is { } tab)
        {
            Run(tab, tab.RaiseUpdate);
        }
    }

    private void GrafUpdateTab(float timeStacker)
    {
        if (_activeTab is { } tab)
        {
            Run(tab, () => tab.RaiseGrafUpdate(timeStacker));
        }
    }

    private void CloseTabs()
    {
        foreach (var tab in _tabs)
        {
            try
            {
                tab.Close();
            }
            catch (Exception exception)
            {
                _logger.LogError($"Friend manager tab {tab.Registration.Id} failed while closing", exception);
            }
        }

        _tabs.Clear();
    }

    private bool Run(FriendTab tab, Action action)
    {
        if (tab.Failed)
        {
            return false;
        }

        try
        {
            action();

            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError($"Friend manager tab {tab.Registration.Id} threw an exception and was disabled", exception);

            tab.Failed = true;

            try
            {
                tab.SetActive(false);
            }
            catch (Exception)
            {
                tab.Container.isVisible = false;
            }

            if (_activeTab == tab)
            {
                _activeTab = null;
                ShowTab(FirstShownTab());
            }

            LayoutTabs();

            return false;
        }
    }
}
