using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Options;

public abstract partial class MenuBuilder
{
    private protected class SectionFilter
    {
        private Section[] _sections = [];

        private OpScrollBox? _scrollBox;

        private float _height;

        private float _view = CanvasSize;

        private string _query = "";

        private string _restored = "";

        private float[] _heights = [];

        private bool[] _visible = [];

        private UIelement? _focus;

        private bool _isPlacing;

        private bool _isSearching;

        internal string Query => _query;

        public void Apply(string query)
        {
            _query = query.Trim();
            _isSearching = true;
            _focus = null;

            Place(true);
        }

        public void Refresh() => Place(false);

        internal void ScrollToTop() => Place(true);

        internal void UpdateLayout()
        {
            bool moving = false;

            foreach (var section in _sections)
            {
                moving |= section.UpdateFold();
            }

            if (moving)
            {
                Place(_isSearching);
                Align();
            }
            else
            {
                _isSearching = false;
                _focus = null;
            }
        }

        internal void Focus(UIelement? anchor)
        {
            _focus = anchor;

            Align();
        }

        internal void Restore(string query) => _restored = query;

        internal void Bind(MenuBuilder builder, OpScrollBox? scrollBox, float height, float view)
        {
            _sections = [.. builder._sections];
            _scrollBox = scrollBox;
            _height = height;
            _view = view;
            _heights = new float[_sections.Length];
            _visible = new bool[_sections.Length];
            _query = _restored.Trim();

            foreach (var section in _sections)
            {
                section.Settle();
            }

            Place(true);
        }

        private void Align()
        {
            if (_focus is not { isRectangular: true } anchor || _scrollBox is not { } scrollBox)
            {
                return;
            }

            scrollBox.targetScrollOffset = Mathf.Clamp(scrollBox.size.y - anchor.GetPos().y - anchor.size.y, scrollBox.MaxScroll, 0f);
        }

        private void Place(bool toTop)
        {
            if (_isPlacing)
            {
                return;
            }

            _isPlacing = true;

            try
            {
                PlaceSections(toTop);
            }
            finally
            {
                _isPlacing = false;
            }
        }

        private void PlaceSections(bool toTop)
        {
            bool changed = false;

            for (int index = 0; index < _sections.Length; index++)
            {
                bool visible = _sections[index].Match(_query);
                float height = visible ? _sections[index].Current : 0f;

                changed |= _visible[index] != visible || _heights[index] != height;
                _visible[index] = visible;
                _heights[index] = height;
            }

            if (!toTop && !changed)
            {
                return;
            }

            float hidden = 0f;

            for (int index = 0; index < _sections.Length; index++)
            {
                hidden += _sections[index].Height - _heights[index];
            }

            float content = Mathf.Clamp(_height - hidden, _view, 10000f);
            float shift = _scrollBox == null ? 0f : content - _height;

            _scrollBox?.SetContentSize(content, sortToTop: false);

            float offset = 0f;

            for (int index = 0; index < _sections.Length; index++)
            {
                _sections[index].Place(_visible[index], shift + offset);

                offset += _sections[index].Height - _heights[index];
            }

            if (_scrollBox is { } scrollBox)
            {
                if (toTop)
                {
                    scrollBox.ScrollToTop();
                }
                else
                {
                    scrollBox.targetScrollOffset = Mathf.Clamp(scrollBox.targetScrollOffset, scrollBox.MaxScroll, 0f);
                }

                scrollBox._MoveCam();
            }
        }
    }
}
