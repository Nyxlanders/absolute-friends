using Menu.Remix.MixedUI;

namespace AbsoluteFriends.Options;

public abstract partial class MenuBuilder
{
    private class OptionEntry(UIfocusable focusable, OpLabel? label, string description)
    {
        private string _mark = "";

        private string _requirement = "";

        public UIfocusable Focusable => focusable;

        public OpLabel? Label => label;

        public bool IsFailed { get; private set; }

        public void SetGreyedOut(bool greyedOut)
        {
            focusable.greyedOut = greyedOut;

            if (focusable is OpRadioButtonGroup radioButtonGroup)
            {
                radioButtonGroup.greyedOut = greyedOut;
            }
        }

        public void SetMark(string text, bool failed)
        {
            IsFailed |= failed;

            if (_mark == text)
            {
                return;
            }

            _mark = text;

            Refresh();
        }

        public void SetRequirement(string text)
        {
            if (_requirement == text)
            {
                return;
            }

            _requirement = text;

            Refresh();
        }

        private void Refresh()
        {
            string[] notes = [description, _mark, _requirement];
            string text = string.Join("\n", notes.Where(note => note.Length > 0));

            Focusable.description = text;

            if (Label is { } opLabel)
            {
                opLabel.description = text;
            }
        }
    }
}
