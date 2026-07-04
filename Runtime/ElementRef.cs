using Armoury.UI.Injectors;
using UnityEngine.UIElements;

namespace Armoury.UI
{
    public class ElementRef : ElementRef<VisualElement>
    {
        public ElementRef(VisualElement element, Injector injector) : base(element, injector) { }
        
        public new class Provider : ValueProvider<ElementRef>
        {
            public Provider(ElementRef elementRef)
            {
                Value = elementRef;
                IsTyped = true;
                TypeToken = typeof(ElementRef);
            }
        }
    }

    public class ElementRef<T> : Injectable where T : VisualElement
    {
        public readonly T VisualElement;

        public ElementRef(T element, Injector injector) : base(injector)
        {
            VisualElement = element;
        }

        protected override void OnInjected() { }
        
        public class Provider : ValueProvider<ElementRef<T>>
        {
            public Provider(ElementRef<T> elementRef)
            {
                Value = elementRef;
                IsTyped = true;
                TypeToken = typeof(ElementRef<T>);
            }
        }
    }
}