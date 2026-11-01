using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Options;

public abstract partial class MenuBuilder
{
    private const string CheckSymbol = "Menu_Symbol_CheckBox";

    private const string MixedSymbol = "pixel";

    private protected OpSimpleImageButton? AddSwitch(string text, string description, Func<SwitchState> state, Func<bool> available, Action toggle, float span = 1f, string? meadowNote = null)
    {
        if (!BeginElement(span))
        {
            return null;
        }

        string translated = WithMeadowNote(Translate(description), meadowNote);
        OpSimpleImageButton button = new(
            new Vector2(_pos.x, _pos.y - Spacing * 0.5f),
            new Vector2(Spacing, Spacing),
            CheckSymbol
        )
        {
            description = translated
        };
        OpLabel label = new(
            new Vector2(_pos.x + Spacing + Gap, _pos.y - Spacing * 0.5f),
            new(ElementWidth * span - Spacing - Gap, Spacing),
            WithMeadowMark(Translate(text), meadowNote is DisabledInMeadowNote or PartlyDisabledInMeadowNote),
            FLabelAlignment.Left
        )
        {
            description = translated
        };

        void UpdateSwitch()
        {
            SwitchState current = state();
            bool mixed = current == SwitchState.Mixed;
            bool greyedOut = !available();

            if (button.greyedOut != greyedOut)
            {
                button.greyedOut = greyedOut;
            }

            button.ChangeElement(mixed ? MixedSymbol : CheckSymbol);
            button.sprite.scaleX = mixed ? 12f : 1f;
            button.sprite.scaleY = mixed ? 2f : 1f;
            button.sprite.isVisible = current != SwitchState.Off;
        }

        button.OnClick += _ =>
        {
            if (!button.greyedOut)
            {
                toggle();
                UpdateSwitch();
            }
        };

        UpdateSwitch();
        Watch(UpdateSwitch);

        AddColumn(span);
        AddElements(button, label);

        return button;
    }

    private protected enum SwitchState
    {
        Off,
        Mixed,
        On,
    }
}
