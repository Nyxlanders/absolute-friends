using AbsoluteFriends.Core;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Manager;

internal partial class FriendManagerDialog
{
    private partial class DetailsTab
    {
        private const float ButtonSize = 24f;

        private const float NameX = 35f;

        private const float ForgetWidth = 120f;

        private const float TitleHeight = 30f;

        private const float FooterHeight = 52f;

        private readonly FriendManagerDialog _dialog;

        private readonly FriendTab _tab;

        private readonly OpImage _icon;

        private readonly OpLabel _name;

        private readonly NicknameTextBox _nickname;

        private readonly OpSimpleImageButton _clear;

        private readonly OpLabel _relationshipsTitle;

        private readonly ScrollArea _relationships;

        private readonly OpLabel _noRelationships;

        private readonly List<RelationshipRow> _rows = [];

        private readonly OpImage _divider;

        private readonly HoldButton _forget;

        private AbstractCreature? _shownFriend;

        internal DetailsTab(FriendManagerDialog dialog, FriendTab tab)
        {
            _dialog = dialog;
            _tab = tab;

            float width = tab.Size.x;
            float nameY = tab.Size.y - ButtonSize;
            float relationshipsTitleY = nameY - 50f;
            float relationshipsHeight = relationshipsTitleY - 8f - FooterHeight;

            _icon = new(new Vector2(ButtonSize * 0.5f, nameY + ButtonSize * 0.5f), "Futile_White")
            {
                anchor = new(0.5f, 0.5f)
            };
            _name = new(new Vector2(NameX, nameY), new Vector2(width - NameX, ButtonSize), string.Empty, FLabelAlignment.Left);
            _nickname = new(new Configurable<string>(string.Empty, new ConfigurableInfo("Edit this friend's nickname.")), new Vector2(NameX, nameY), width - NameX - ButtonSize - 6f, TypeNickname)
            {
                allowSpace = true,
                maxLength = FriendNicknames.MaxNicknameLength,
                description = Translation.Of("Edit this friend's nickname.")
            };
            _clear = new(new Vector2(width - ButtonSize, nameY), new Vector2(ButtonSize, ButtonSize), "Menu_Symbol_Clear_All")
            {
                description = Translation.Of("Clear this nickname.")
            };
            _relationshipsTitle = new(new Vector2(0f, relationshipsTitleY), new Vector2(width, TitleHeight), Translation.Of("Friend Relationships"), FLabelAlignment.Center, true);
            _relationships = new(new Vector2(0f, FooterHeight), new Vector2(width + MenuBuilder.Padding, relationshipsHeight));
            _noRelationships = new(new Vector2(0f, FooterHeight), new Vector2(width, relationshipsHeight), Translation.Of("No other friends"), FLabelAlignment.Center)
            {
                color = MenuColorEffect.rgbMediumGrey
            };
            _divider = new(new Vector2(0f, FooterHeight - 8f), "pixel")
            {
                scale = new(width, 1f),
                color = MenuColorEffect.rgbMediumGrey
            };
            _forget = new(new Vector2(width - ForgetWidth, 0f), new Vector2(ForgetWidth, ButtonSize), Translation.Of("Forget Friend"))
            {
                description = Translation.Of("Hold to forget this friend. Clears its like toward every slugcat and its nickname, and stops tracking it.")
            };

            _clear.OnClick += delegate { SetNickname(null); };
            _forget.Done += Forget;

            foreach (var element in (UIelement[])[_icon, _name, _nickname, _clear, _relationshipsTitle, _relationships, _noRelationships, _divider, _forget])
            {
                tab.Add(element);
            }

            tab.FriendChanged += LoadFriend;
            tab.Refresh += Refresh;
        }

        private void LoadFriend(AbstractCreature? friend)
        {
            if (friend is { IsPlayer: false })
            {
                _nickname.value = friend.FriendName;
            }
        }

        private void Refresh()
        {
            AbstractCreature? friend = _tab.Friend;
            bool named = friend is { IsPlayer: false };
            bool forgettable = friend is { IsForgettable: true };
            (string symbol, Color color) = friend?.Icon ?? (string.Empty, Color.white);

            SetShown(_icon, friend != null && Futile.atlasManager.DoesContainElementWithName(symbol));
            SetShown(_name, friend is { IsPlayer: true });

            SetShown(named, _nickname, _clear);
            SetShown(friend != null, _relationshipsTitle, _relationships);
            SetShown(forgettable, _divider, _forget);

            RefreshRelationships(friend);

            if (friend == null)
            {
                return;
            }

            bool readOnly = FriendManager.IsReadOnly;

            if (!_icon.Hidden)
            {
                _icon.sprite.SetElementByName(symbol);
                _icon.color = color;
                FitIcon(_icon);
            }

            _name.text = friend.PlayerName;
            _name.color = friend.ShortcutColor;
            _nickname.colorText = friend.ShortcutColor;

            foreach (var element in (UIfocusable[])[_nickname, _clear])
            {
                element.greyedOut = readOnly;
            }

            if (named && !_nickname._KeyboardOn && _nickname.value != friend.FriendName)
            {
                _nickname.value = friend.FriendName;
            }
        }

        private void RefreshRelationships(AbstractCreature? friend)
        {
            List<AbstractCreature> others = friend == null ? [] : [.. _dialog._rows.Select(row => row.Creature).Where(other => other != friend)];

            while (_rows.Count < others.Count)
            {
                _rows.Add(new RelationshipRow(_tab, _relationships));
            }

            _relationships.ContentHeight = others.Count * RelationshipRow.Height;

            if (friend != _shownFriend)
            {
                _shownFriend = friend;
                _relationships.ScrollToTop();
            }

            for (int index = 0; index < _rows.Count; index++)
            {
                if (friend != null && index < others.Count)
                {
                    _rows[index].Show(friend, others[index], _relationships.Top - (index + 1) * RelationshipRow.Height);
                }
                else
                {
                    _rows[index].Hide();
                }
            }

            SetShown(_noRelationships, friend != null && others.Count == 0);
        }

        private void SetNickname(string? value)
        {
            if (_tab.Friend is not { IsPlayer: false } friend)
            {
                return;
            }

            if (value != friend.FriendName)
            {
                friend.SetNickname(value == friend.DefaultName ? null : value);
            }

            _nickname.value = friend.FriendName;
            _tab.RefreshManager();
        }

        private void TypeNickname(string value)
        {
            if (_tab.Friend is not { IsPlayer: false } friend || value == friend.FriendName)
            {
                return;
            }

            friend.SetNickname(value == friend.DefaultName ? null : value);
            _tab.RefreshManager();
        }

        private void Forget()
        {
            if (_tab.Friend is not { IsPlayer: false } friend)
            {
                return;
            }

            friend.Forget();
            _tab.RefreshManager();
            Refresh();
        }

    }

    private class NicknameTextBox(ConfigurableBase configurable, Vector2 position, float width, Action<string> typed) : OpTextBox(configurable, position, width)
    {
        public override bool CurrentlyFocusableNonMouse => false;

        public override string value
        {
            get => _value;
            set
            {
                value ??= string.Empty;

                if (value.Length > maxLength)
                {
                    value = value.Substring(0, maxLength);
                }

                if (_value != value)
                {
                    _value = value;
                    Change();

                    if (_KeyboardOn)
                    {
                        typed(value);
                    }
                }
            }
        }
    }
}
