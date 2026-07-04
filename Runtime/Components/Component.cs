using UnityEngine;
using UnityEngine.UIElements;
using Armoury.UI.Injectors;

namespace Armoury.UI
{
    public abstract class Component : Directive
    {
        protected Component(Injector injector) : base(injector) { }
    }

    [System.Serializable]
    public class ComponentMetadata<T> where T : Component
    {
        [SerializeField] private VisualTreeAsset _uxml;
        public VisualTreeAsset UXML => _uxml;

        public ComponentMetadata(VisualTreeAsset uxml)
        {
            if (uxml != null)
            {
                _uxml = uxml;
            }
        }

        public ComponentMetadata() : this(null) { }

        public abstract class Provider : ValueProvider<ComponentMetadata<T>>
        {
            public Provider()
            {
                IsTyped = true;
                TypeToken = typeof(ComponentMetadata<T>);
            }
        }
    }
}