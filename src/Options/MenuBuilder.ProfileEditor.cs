using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Options;

public abstract partial class MenuBuilder
{
    private protected class ProfileEditor
    {
        private readonly UIelement _display;

        private readonly UIelement[] _actions;

        private readonly Action<string> _retitle;

        private readonly OpTextBox _textBox;

        private readonly OpSimpleButton _save;

        private readonly OpSimpleButton _cancel;

        private string _value;

        internal ProfileEditor(UIelement display, UIelement[] actions, Action<string> retitle, OpTextBox textBox, OpSimpleButton save, OpSimpleButton cancel, string value)
        {
            _display = display;
            _actions = actions;
            _retitle = retitle;
            _textBox = textBox;
            _save = save;
            _cancel = cancel;
            _value = value;
        }

        internal bool IsEditing { get; private set; }

        internal string Value => _textBox.value;

        internal void Begin()
        {
            _textBox.value = _value.All(character => character is >= ' ' and <= '~') ? _value : "";
            IsEditing = true;
            RefreshVisibility(true);
        }

        internal void Cancel()
        {
            IsEditing = false;
            RefreshVisibility(true);
        }

        internal void Refresh(string value)
        {
            _value = value;
            _retitle(value);

            if (!IsEditing && value.All(character => character is >= ' ' and <= '~'))
            {
                _textBox.value = value;
            }
        }

        internal void RefreshVisibility(bool visible)
        {
            SetVisible(_display, visible && !IsEditing);

            foreach (var action in _actions)
            {
                SetVisible(action, visible && !IsEditing);
            }

            SetVisible(_textBox, visible && IsEditing);
            SetVisible(_save, visible && IsEditing);
            SetVisible(_cancel, visible && IsEditing);
        }

        private static void SetVisible(UIelement element, bool visible)
        {
            if (visible)
            {
                element.Show();
            }
            else
            {
                element.Hide();
            }
        }
    }

    private class ProfileNameTextBox(ConfigurableBase configurable, Vector2 pos, float sizeX) : OpTextBox(configurable, pos, sizeX)
    {
        public override string value
        {
            get => _value;
            set
            {
                value ??= "";

                if (value.Length > maxLength)
                {
                    value = value.Substring(0, maxLength);
                }

                if (_value != value)
                {
                    _value = value;
                    Change();
                }
            }
        }
    }

    private protected static Configurable<T> CreateTransientConfigurable<T>(T value, ConfigurableInfo info)
    {
        return (Configurable<T>)Activator.CreateInstance(typeof(Configurable<T>), value, info);
    }

    private protected ProfileEditor? AddProfileCreator(string text, string description, string value, Action<string> save, float span = 1f)
    {
        if (!BeginElement(span))
        {
            return null;
        }

        string note = Translate(description);
        float width = ElementWidth * span - Gap * 2f;
        float actionWidth = Spacing;
        float editorWidth = width - actionWidth * 2f - Gap * 2f;
        Vector2 pos = new(_pos.x + Gap, _pos.y - Spacing * 0.5f);
        OpSimpleButton button = new(pos, new Vector2(width, Spacing), Translate(text))
        {
            description = note
        };
        OpTextBox textBox = CreateProfileNameTextBox(value, note, pos, editorWidth);
        OpSimpleImageButton saveButton = CreateProfileImageButton(pos.x + editorWidth + Gap, pos.y, "Menu_Symbol_CheckBox", "Saves the entered profile name.");
        OpSimpleImageButton cancelButton = CreateProfileImageButton(pos.x + editorWidth + actionWidth + Gap * 2f, pos.y, "Menu_Symbol_Clear_All", "Cancels editing the profile name.");
        ProfileEditor editor = new(button, [], text => button.text = text, textBox, saveButton, cancelButton, value);

        button.OnClick += delegate
        {
            editor.Begin();
        };
        BindProfileEditor(editor, saveButton, cancelButton, save);
        AddColumn(span);
        AddElements(button, textBox, saveButton, cancelButton);
        editor.RefreshVisibility(true);

        return editor;
    }

    private protected ProfileEditor AddProfileEditor(OpLabel label, string description, string value, Action<string> save)
    {
        string note = Translate(description);
        float width = label.size.x - Gap;
        float actionWidth = Spacing;
        float editorWidth = width - actionWidth * 2f - Gap * 2f;
        float actionsWidth = Spacing + Gap;

        label.size = new Vector2(width - actionsWidth, label.size.y);

        OpTextBox textBox = CreateProfileNameTextBox(value, note, label.pos, editorWidth);
        OpSimpleImageButton saveButton = CreateProfileImageButton(label.pos.x + editorWidth + Gap, label.pos.y, "Menu_Symbol_CheckBox", "Saves the entered profile name.");
        OpSimpleImageButton cancelButton = CreateProfileImageButton(label.pos.x + editorWidth + actionWidth + Gap * 2f, label.pos.y, "Menu_Symbol_Clear_All", "Cancels editing the profile name.");
        OpSimpleImageButton renameButton = CreateProfileImageButton(label.pos.x + width - actionsWidth + Gap, label.pos.y, "modInputCursor", description);
        ProfileEditor editor = new(label, [renameButton], text => label.text = text, textBox, saveButton, cancelButton, value);

        renameButton.OnClick += delegate
        {
            editor.Begin();
        };
        BindProfileEditor(editor, saveButton, cancelButton, save);
        AddElements(textBox, saveButton, cancelButton, renameButton);
        editor.RefreshVisibility(true);

        return editor;
    }

    private static void BindProfileEditor(ProfileEditor editor, OpSimpleButton saveButton, OpSimpleButton cancelButton, Action<string> save)
    {
        saveButton.OnClick += delegate
        {
            save(editor.Value);
            editor.Cancel();
        };
        cancelButton.OnClick += delegate
        {
            editor.Cancel();
        };
    }

    private OpTextBox CreateProfileNameTextBox(string value, string description, Vector2 pos, float width)
    {
        Configurable<string> configurable = CreateTransientConfigurable(
            value,
            new ConfigurableInfo(description)
        );

        return new ProfileNameTextBox(configurable, pos, width)
        {
            allowSpace = true,
            maxLength = 80,
            description = description
        };
    }

    private OpSimpleImageButton CreateProfileImageButton(float x, float y, string image, string description)
    {
        return new(new Vector2(x, y), new Vector2(Spacing, Spacing), image)
        {
            description = Translate(description)
        };
    }
}
