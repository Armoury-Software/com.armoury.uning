using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.UIElements;

namespace Armoury.UI.Markers.Structural
{
    [UxmlElement]
    public partial class NgIfDirectiveMarker : StructuralDirectiveMarker
    {
        private static readonly BindingId ConditionProperty = nameof(Condition);
        private static readonly BindingId IsInvertedProperty = nameof(IsInverted);

        private readonly List<(VisualElement Element, StyleEnum<DisplayStyle> OriginalDisplay)> _controlledChildren = new();
        
#if UNITY_EDITOR
        private readonly List<(VisualElement Element, StyleEnum<DisplayStyle> OriginalDisplay)> _editorPreviewChildren = new();
#endif

        private bool _condition = true;
        [UxmlAttribute("condition"), CreateProperty] public bool Condition
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

        private bool _isInverted;
        [UxmlAttribute("invert"), CreateProperty] public bool IsInverted
        {
            get => _isInverted;
            set
            {
                if (_isInverted == value)
                    return;

                _isInverted = value;
                ApplyCondition();

                NotifyPropertyChanged(IsInvertedProperty);
            }
        }
        
        public NgIfDirectiveMarker()
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                RegisterCallback<AttachToPanelEvent>(_ => ApplyCondition());
                schedule.Execute(ApplyCondition);
            }
#endif
        }

        public void Unwrap()
        {
            if (IsCompiled)
                return;

            var targetParent = parent;

            if (targetParent == null)
                return;

            IsCompiled = true;

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
            if (IsCompiled)
            {
                foreach (var entry in _controlledChildren)
                    ApplyCondition(entry.Element, entry.OriginalDisplay);

                return;
            }

#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                ApplyEditorPreviewCondition();
            }
#endif
        }

#if UNITY_EDITOR
        private void ApplyEditorPreviewCondition()
        {
            TrackCurrentTemplateChildren();

            foreach (var entry in _editorPreviewChildren)
            {
                if (entry.Element.hierarchy.parent != this)
                    continue;

                ApplyCondition(entry.Element, entry.OriginalDisplay);
            }
        }

        private void TrackCurrentTemplateChildren()
        {
            for (var i = 0; i < childCount; i++)
            {
                var child = this[i];

                if (IsAlreadyTrackedForEditorPreview(child))
                    continue;

                _editorPreviewChildren.Add((child, child.style.display));
            }
        }

        private bool IsAlreadyTrackedForEditorPreview(VisualElement child)
        {
            foreach (var entry in _editorPreviewChildren)
            {
                if (entry.Element == child)
                    return true;
            }

            return false;
        }
#endif

        private void ApplyCondition(
            VisualElement child,
            StyleEnum<DisplayStyle> originalDisplay
        )
        {
            child.style.display = (!IsInverted ? _condition : !_condition)
                ? originalDisplay
                : DisplayStyle.None;
        }
    }
}
