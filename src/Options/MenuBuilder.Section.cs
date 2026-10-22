using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Options;

public abstract partial class MenuBuilder
{
    private class Section(string?[] names, int index, float top, Func<bool>? gate)
    {
        private const float Ease = 0.3f;

        private UIelement[] _elements = [];

        private Vector2[] _positions = [];

        private OpRect? _container;

        private Vector2 _containerSize;

        private Func<bool>? _unfolded;

        private int _foldIndex = -1;

        private float _foldTop;

        private float _shown = 1f;

        public int Index => index;

        public float Height { get; private set; }

        public float FoldExtent { get; private set; }

        public float Current => Height - (1f - _shown) * FoldExtent;

        private bool IsUnfolded => _unfolded?.Invoke() ?? true;

        public void Fold(OpRect container, Func<bool> unfolded, int foldIndex, float foldTop)
        {
            _container = container;
            _unfolded = unfolded;
            _foldIndex = foldIndex;
            _foldTop = foldTop;
        }

        public void Close(UIelement[] elements, float bottom)
        {
            _elements = elements;

            Height = top - bottom;
            FoldExtent = _container == null ? 0f : Mathf.Max(_foldTop - _container.GetPos().y, 0f);
        }

        public void Anchor()
        {
            _positions = [.. _elements.Select(element => element.GetPos())];
            _containerSize = _container?.size ?? Vector2.zero;
        }

        public void Settle()
        {
            _shown = IsUnfolded ? 1f : 0f;
        }

        public bool UpdateFold()
        {
            if (_foldIndex < 0)
            {
                return false;
            }

            float target = IsUnfolded ? 1f : 0f;

            if (_shown == target)
            {
                return false;
            }

            _shown = Mathf.Abs(target - _shown) < 0.01f ? target : Mathf.Lerp(_shown, target, Ease);

            return true;
        }

        public bool Match(string query)
        {
            if (gate?.Invoke() == false)
            {
                return false;
            }

            if (query.Length == 0)
            {
                return true;
            }

            bool hasName = false;

            foreach (var name in names)
            {
                if (name == null)
                {
                    continue;
                }

                hasName = true;

                if (name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return !hasName;
        }

        public void Place(bool visible, float offset)
        {
            float folded = (1f - _shown) * FoldExtent;

            for (int index = 0; index < _elements.Length; index++)
            {
                UIelement element = _elements[index];
                Vector2 pos = _positions[index] + new Vector2(0f, offset);

                if (element == _container)
                {
                    element.SetPos(pos + new Vector2(0f, folded));
                    element.size = new(_containerSize.x, Mathf.Max(_containerSize.y - folded, 0f));
                }
                else
                {
                    element.SetPos(pos);
                }

                Snap(element);

                if (visible && (_foldIndex < 0 || index < _foldIndex || _shown >= 1f))
                {
                    element.Show();
                }
                else
                {
                    element.Hide();
                }
            }
        }
    }
}
