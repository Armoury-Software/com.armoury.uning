using System.Collections;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Armoury.UI.Markers.Structural
{
    [UxmlElement]
    public partial class NgForDirectiveMarker : StructuralDirectiveMarker
    {
        private static readonly BindingId ItemsSourceProperty = nameof(ItemsSource);

        private readonly List<VisualTreeAsset> _templates = new();
        private readonly List<VisualElement> _instances = new();

        private VisualElement _targetParent;
        private bool _isCompiled;

        private object _itemsSource;

        [CreateProperty]
        public object ItemsSource
        {
            get => _itemsSource;
            set
            {
                if (ReferenceEquals(_itemsSource, value))
                    return;

                _itemsSource = value;
                Rebuild();

                NotifyPropertyChanged(ItemsSourceProperty);
            }
        }

        public void Compile()
        {
            if (_isCompiled)
                return;

            _targetParent = parent;

            if (_targetParent == null)
                return;

            _isCompiled = true;

            CaptureTemplatesFromChildren();

            style.display = DisplayStyle.None;
            pickingMode = PickingMode.Ignore;

            Rebuild();
        }

        private void CaptureTemplatesFromChildren()
        {
            _templates.Clear();

            for (var i = 0; i < childCount; i++)
            {
                var child = this[i];

                if (!TryGetTemplate(child, out var template))
                {
                    Debug.LogError(
                        $"NgForDirectiveMarker only supports UXML template children. " +
                        $"Invalid child: {child.GetType().Name} / '{child.name}'."
                    );

                    continue;
                }

                _templates.Add(template);
            }

            if (_templates.Count == 0)
            {
                Debug.LogError(
                    "NgForDirectiveMarker needs at least one UXML template child.",
                    null
                );
            }
        }

        private static bool TryGetTemplate(
            VisualElement child,
            out VisualTreeAsset template
        )
        {
            template = null;

            if (child is TemplateContainer templateContainer)
            {
                template =
                    templateContainer.templateSource ??
                    templateContainer.visualTreeAssetSource;

                return template != null;
            }

            template = child.visualTreeAssetSource;
            return template != null;
        }

        private void Rebuild()
        {
            if (!_isCompiled || _targetParent == null)
                return;

            ClearInstances();

            if (_templates.Count == 0)
                return;

            if (_itemsSource is not IEnumerable enumerable)
                return;

            var insertIndex = _targetParent.IndexOf(this) + 1;

            var index = 0;

            foreach (var item in enumerable)
            {
                foreach (var template in _templates)
                {
                    var instance = template.Instantiate();

                    instance.dataSource = new NgForItemContext(
                        item: item,
                        index: index
                    );

                    _targetParent.Insert(insertIndex++, instance);
                    _instances.Add(instance);
                }

                index++;
            }
        }

        private void ClearInstances()
        {
            foreach (var instance in _instances)
                instance.RemoveFromHierarchy();

            _instances.Clear();
        }
    }
    
    public sealed class NgForItemContext
    {
        [CreateProperty]
        public object Item { get; }

        [CreateProperty]
        public int Index { get; }

        public NgForItemContext(object item, int index)
        {
            Item = item;
            Index = index;
        }
    }
}