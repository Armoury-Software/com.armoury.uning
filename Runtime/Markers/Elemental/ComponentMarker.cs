using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using Armoury.UI.Markers.Elemental.Editor;
#endif

namespace Armoury.UI.Markers.Elemental
{
    [UxmlElement]
    public abstract partial class ComponentMarker<TComp> : ComponentMarker
        where TComp : Component
    {
        public override System.Type ComponentType => typeof(TComp);

        public ComponentMarker()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EnsureComponentDefinition();
                RegisterCallback<AttachToPanelEvent>(_ => EnsureComponentDefinition());
                schedule.Execute(EnsureComponentDefinition);

                ComponentTemplatePreview.InstantiateInto<TComp>(this);
            }
#endif
        }

#if UNITY_EDITOR
        private void EnsureComponentDefinition()
        {
            dataSourceType = typeof(TComp);

            Component ??= new ComponentDefinition();
        }
#endif
    }

    [UxmlElement]
    public partial class ComponentMarker : ElementMarker
    {
        [UxmlObjectReference("uning-component")]
        public ComponentDefinition Component { get; set; }
        
        public virtual System.Type ComponentType { get; protected set; }

        public static ComponentMarker Instantiate(in Definition definition)
        {
            return new ComponentMarker
            {
                ComponentType = definition.ComponentType
            };
        }
        
        // TODO: Make abstract
        public Definition CreateDefinition() => new(ComponentType);
        
        public struct Definition
        {
            public readonly System.Type ComponentType;

            public Definition(System.Type type)
            {
                ComponentType = type;
            }
        }
    }
}