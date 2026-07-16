using System.Collections;
using System.Collections.Generic;
using Armoury.UI.Markers.Elemental;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Armoury.UI.Markers.Structural
{
    [UxmlElement]
    public partial class NgForDirectiveMarker : StructuralDirectiveMarker
    {
        private static readonly BindingId ItemsSourceProperty = nameof(ItemsSource);

        private readonly List<VisualElement> _instances = new();
        public List<VisualElement> RemoveMe_Instances => _instances; // TODO: REMOVE THIS! This is part of the band-aid fix
        private readonly List<ComponentMarker.Definition> _definitions = new();
        
        private VisualElement _targetParent;
        
        private object _itemsSource;
        [CreateProperty] public object ItemsSource
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

        public NgForDirectiveMarker()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EnsureValidHierarchy();
                RegisterCallback<AttachToPanelEvent>(_ => EnsureValidHierarchy());
                schedule.Execute(EnsureValidHierarchy);
            }
#endif
        }
        
        public bool Compile()
        {
            if (IsCompiled)
                return false;

            _targetParent = hierarchy.parent;

            if (_targetParent == null)
                return false;

            IsCompiled = true;

            CaptureDefinitions();

            style.display = DisplayStyle.None;
            pickingMode = PickingMode.Ignore;

            return Rebuild();
        }
        
        private void CaptureDefinitions()
        {
            _definitions.Clear();

            while (childCount > 0)
            {
                var child = this[0];

                if (Marker.Is<ComponentMarker>(child, out var componentMarker))
                {
                    _definitions.Add(componentMarker.CreateDefinition());
                }
                else
                {
                    Debug.LogError(
                        "[NgForDirectiveMarker (CaptureDefinitions):] Currently, NgFor is restricted to only contain " +
                        "component markers as children.");
                }
                
                this[0].RemoveFromHierarchy();
            }

            if (_definitions.Count == 0)
            {
                Debug.LogError(
                    "[NgForDirectiveMarker (CaptureDefinitions):] NgFor needs at least one component marker child.",
                    null
                );
            }
        }
        
        private bool Rebuild()
        {
            if (!IsCompiled || _targetParent == null)
                return false;

            ClearInstances();

            if (_definitions.Count == 0)
                return false;

            if (_itemsSource is not IEnumerable enumerable)
                return false;

            var insertIndex = _targetParent.hierarchy.IndexOf(this) + 1;
            var index = 0;

            foreach (var item in enumerable)
            {
                foreach (var definition in _definitions)
                {
                    var instance = ComponentMarker.Instantiate(in definition);
                    
                    /*instance.dataSource = new NgForItemContext(
                        item: item,
                        index: index
                    );*/
                    
                    _targetParent.hierarchy.Insert(insertIndex++, instance);
                    _instances.Add(instance);
                    
                    // TODO: Even though this creates the marker, it doesn't generate a scaffolding for ViewContainerRef
                }

                index++;
            }

            return true;
        }

        private void ClearInstances()
        {
            foreach (var instance in _instances)
                instance.RemoveFromHierarchy();

            _instances.Clear();
        }
        
#if UNITY_EDITOR
        private void EnsureValidHierarchy()
        {
            if (childCount == 0) return;
            EnsureSingleDescendant();
            EnsureOnlyMarkerChildren();
        }

        private void EnsureSingleDescendant()
        {
            if (childCount > 1)
            {
                Debug.LogError(
                    "[NgForDirectiveMarker (EnsureValidHierarchy):] Currently, NgFor is restricted to only 1 child.");
                while (childCount > 1)
                {
                    Remove(this[1]);
                }
            }
        }

        private void EnsureOnlyMarkerChildren()
        {
            var i = 0;
            while (i < childCount)
            {
                if (!Marker.Is<ComponentMarker>(this[i]))
                {
                    Debug.LogError(
                        "[NgForDirectiveMarker (EnsureValidHierarchy):] Currently, NgFor is restricted to only contain " +
                        $"component markers as children. {this[i].GetType().Name} {this[i].name} will be removed.");

                    Remove(this[i]);
                    
                    continue;
                }
                
                i++;
            }
        }
#endif
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