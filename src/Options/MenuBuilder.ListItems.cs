using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Options;

public abstract partial class MenuBuilder
{
    private class StyledComboBox(Configurable<string> option, Vector2 position, float width, List<ListItem> items, Action<ListItem, FLabel, bool> styleItem)
        : OpComboBox(option, position, width, items)
    {
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            ApplyListItemStyle(_lblText, _lblList, _itemList, _searchMode ? _searchList : _itemList, _listTop, value, styleItem);
        }
    }

    private class StyledListBox(Configurable<string> option, Vector2 position, float width, List<ListItem> items, ushort visibleItems, Action<ListItem, FLabel, bool> styleItem)
        : OpListBox(option, position, width, items, visibleItems, true)
    {
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            ApplyListItemStyle(_lblText, _lblList, _itemList, _searchMode ? _searchList : _itemList, _listTop, value, styleItem);
        }
    }

    private static void ApplyListItemStyle(FLabel? selected, FLabel[]? labels, IReadOnlyList<ListItem>? allItems, IReadOnlyList<ListItem>? visibleItems, int top, string value, Action<ListItem, FLabel, bool> styleItem)
    {
        if (selected != null && allItems != null)
        {
            foreach (var item in allItems)
            {
                if (item.name == value)
                {
                    styleItem(item, selected, false);
                    break;
                }
            }
        }

        if (labels == null || visibleItems == null)
        {
            return;
        }

        for (int index = 0; index < labels.Length; index++)
        {
            int itemIndex = top + index;

            if (itemIndex >= 0 && itemIndex < visibleItems.Count && labels[index] is { } label)
            {
                ListItem item = visibleItems[itemIndex];

                styleItem(item, label, item.name == value);
            }
        }
    }
}
