using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.UIElements;

namespace Armoury.UI.Markers.Structural
{
    [UxmlElement]
    public partial class NgIfDirectiveMarker : StructuralDirectiveMarker
    {
        private static readonly BindingId ConditionProperty = nameof(Condition);

        private readonly List<(VisualElement Element, StyleEnum<DisplayStyle> OriginalDisplay)> _controlledChildren = new();

        private bool _isUnwrapped;
        private bool _condition = true;

        [UxmlAttribute("condition"), CreateProperty]
        public bool Condition
        {
            get => _condition;
            set
            {
                if (_condition == value)
                    return;

                _condition = value;
                ApplyCondition();

                NotifyPropertyChanged(ConditionProperty);
            }
        }

        public void Unwrap()
        {
            if (_isUnwrapped)
                return;

            var targetParent = parent;

            if (targetParent == null)
                return;

            _isUnwrapped = true;

            var insertIndex = targetParent.IndexOf(this) + 1;

            while (childCount > 0)
            {
                var child = this[0];

                Remove(child);
                targetParent.Insert(insertIndex++, child);

                AddControlledChild(child);
            }

            style.display = DisplayStyle.None;
            pickingMode = PickingMode.Ignore;
        }

        private void AddControlledChild(VisualElement child)
        {
            var originalDisplay = child.style.display;

            _controlledChildren.Add((child, originalDisplay));

            ApplyCondition(child, originalDisplay);
        }

        private void ApplyCondition()
        {
            foreach (var entry in _controlledChildren)
                ApplyCondition(entry.Element, entry.OriginalDisplay);
        }

        private void ApplyCondition(
            VisualElement child,
            StyleEnum<DisplayStyle> originalDisplay
        )
        {
            child.style.display = _condition
                ? originalDisplay
                : DisplayStyle.None;
        }
    }
}
