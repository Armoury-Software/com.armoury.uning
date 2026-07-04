using System;
using System.Linq;
using UnityEngine.UIElements;
using Armoury.UI.Injectors;
using Armoury.UI.Markers;
using Armoury.UI.Markers.Elemental;
using Armoury.UI.Markers.Structural;
using UnityEngine;

namespace Armoury.UI
{
    // TODO: Each item of the ViewContainerRef will be a ViewRef or EmbeddedViewRef (EmbeddedViewRef will inherit from ViewRef)
    // TODO: This class will store all ViewRef's/EmbeddedViewRef's so that we can use get(index: number): ViewRef | null
    
    // TODO: ViewContainerRefs should get destroyed eventually. When this happens, child ViewContainerRefs should also be destroyed, ..
    // TODO: .. along with their injectors
    public class ViewContainerRef : Injectable
    {
        /// <summary>
        /// Usually the first added element (not necessarily the first element in the hierarchy of this view)
        /// </summary>
        internal readonly ElementRef anchor;

        private int _length = 1;

        private ViewContainerRef(ElementRef anchor, Injector injector) : base(injector)
        {
            this.anchor = anchor;
        }

        public static ViewContainerRef CreateInPlace<TComp, TElement>(
            TElement root,
            Injector injector
        )
            where TComp : Component, new()
            where TElement : VisualElement
        {
            var anchor = new ElementRef(root, new Injector(injector));
            var viewContainerRef = new ViewContainerRef(anchor, injector);
            
            injector.Inject(new Provider(viewContainerRef));
            
            MakeComponent<TComp, TElement>(anchor);
            
            return viewContainerRef;
        }
        
        public static ViewContainerRef CreateChild<TComp, TElement>(
            VisualElement parent,
            Injector injector
        )
            where TComp : Component, new()
            where TElement : VisualElement
        {
            var innerInjector = new Injector(injector);
            var elementRef = CreateAndInstantiateElementRefFromVisualTree<TComp>(parent, new Injector(innerInjector));
            var viewContainerRef = new ViewContainerRef(elementRef, innerInjector);
            
            innerInjector.Inject(new Provider(viewContainerRef));

            viewContainerRef.CreateComponent<TComp, TElement>(elementRef);
            
            return viewContainerRef;
        }

        private static DirectiveRef<TComp> MakeComponent<TComp, TElement>(
            ElementRef elementRef
        )
            where TComp : Component, new()
            where TElement : VisualElement
        {
            var component = new TComp();
            elementRef.VisualElement.dataSource = component;

            elementRef.Injector.Inject(new ElementRef.Provider(elementRef));

            if (typeof(TElement) != typeof(VisualElement))
            {
                var typedElement = new ElementRef<TElement>((TElement) elementRef.VisualElement, elementRef.Injector);
                elementRef.Injector.Inject(new ElementRef<TElement>.Provider(typedElement));
            }
            
            var directiveRef = new DirectiveRef<TComp>(component, elementRef, elementRef.Injector);
            elementRef.Injector.Inject(new DirectiveRef<TComp>.Provider(directiveRef));

            component.Injector = elementRef.Injector;

            return directiveRef;
        }
        
        public DirectiveRef<TComp> CreateComponent<TComp, TElement>(
            int? index = null
        )
            where TComp : Component, new()
            where TElement : VisualElement
        {
            return CreateComponent<TComp, TElement>(
                CreateAndInstantiateElementRefFromVisualTree<TComp>(anchor.VisualElement.parent, new Injector(Injector)), 
                index: index
            );
        }
        
        private DirectiveRef<TComp> CreateComponent<TComp, TElement>(
            ElementRef elementRef,
            int? index = null
        )
            where TComp : Component, new()
            where TElement : VisualElement
        {
            if (anchor.VisualElement != elementRef.VisualElement)
            {
                if (index != null)
                {
                    anchor.VisualElement.parent.Insert(index.Value, elementRef.VisualElement);
                }
                else
                {
                    anchor.VisualElement.Add(elementRef.VisualElement);
                }

                _length++;
            }

            var directiveRef = MakeComponent<TComp, TElement>(elementRef);

            Scaffold(elementRef.VisualElement);
            
            return directiveRef;
        }

        private void Scaffold(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            ViewContainerRef rootLevelInstance = null;

            ScaffoldChildren(
                parent: root,
                currentLevelInstance: ref rootLevelInstance,
                closestUpperLevelInstance: this
            );
        }

        private static void ScaffoldChildren(
            VisualElement parent,
            ref ViewContainerRef currentLevelInstance,
            ViewContainerRef closestUpperLevelInstance
        )
        {
            ViewContainerRef childLevelInstance = null;

            var i = 0;

            while (i < parent.hierarchy.childCount)
            {
                var child = parent.hierarchy.ElementAt(i);

                // TODO: Currently, NgIfDirectiveMarker only hides elements - it doesn't actually remove them from the
                // TODO: .. DOM. In the future, we might want to actually remove them, and re-instantiate them when
                // TODO: .. NgIfDirectiveMarker's Condition becomes true again.
                if (Marker.Is<NgIfDirectiveMarker>(child, out var ngIfMarker))
                {
                    ngIfMarker.Unwrap();

                    // The marker stays at index i, and its former children are now inserted after it
                    // Move past the hidden marker so the next iteration visits the first unwrapped child
                    i++;

                    continue;
                }
                
                if (Marker.Is<NgForDirectiveMarker>(child, out var ngForMarker))
                {
                    ngForMarker.Compile();

                    // The generated instances are inserted after the marker
                    // Move past the hidden marker; the next loop iteration will naturally visit the first generated...
                    // .. instance
                    i++;
                    
                    continue;
                }

                if (Marker.Is<ComponentMarker>(child, out var componentMarker))
                {
                    HandleScaffolded(
                        marker: componentMarker,
                        currentLevelInstance: ref currentLevelInstance,
                        closestUpperLevelInstance: closestUpperLevelInstance
                    );

                    // Stop processing this branch
                    break;
                }

                ScaffoldChildren(
                    parent: child,
                    currentLevelInstance: ref childLevelInstance,
                    closestUpperLevelInstance: currentLevelInstance ?? closestUpperLevelInstance
                );

                i++;
            }
        }

        private static void HandleScaffolded(
            ComponentMarker marker,
            ref ViewContainerRef currentLevelInstance,
            ViewContainerRef closestUpperLevelInstance)
        {
            if (currentLevelInstance != null)
            {
                Debug.Log($"[ViewContainerRef (HandleScaffolded):] Found EXISTING ViewContainerRef ({currentLevelInstance}). Its anchor is ({currentLevelInstance.anchor.VisualElement.GetType().Name} {currentLevelInstance.anchor.VisualElement.name}).");

                var elementRef = new ElementRef(marker.parent, new Injector(currentLevelInstance.Injector));
                
                typeof(ViewContainerRef)
                    .GetMethod(nameof(MakeComponent), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(
                        marker.Component.Type,
                        marker.parent.GetType()
                    )
                    .Invoke(
                        null,
                        new object[]
                        {
                            elementRef
                        }
                );
                
                Debug.Log($"[ViewContainerRef (HandleScaffolded):] Made NEW component for EXISTING ViewContainerRef ({currentLevelInstance}). The ViewContainerRef anchor is ({currentLevelInstance.anchor.VisualElement.GetType().Name} {currentLevelInstance.anchor.VisualElement.name}). The ElementRef's element is ({elementRef.VisualElement.GetType().Name} {elementRef.VisualElement.name})");
                Debug.Log($"[ViewContainerRef (HandleScaffolded):] The <NEW component for EXISTING ViewContainerRef>'s Injector lists as follows: {string.Join(",", elementRef.Injector.Providers.Select(prov => $"[{prov.GetType().Name}:] {prov.StringToken}/{prov.TypeToken}"))}");
                
                return;
            }
            
            // If we don't want to use Reflection in the future, we could create "creator" classes for each component
            var method = typeof(ViewContainerRef)
                .GetMethod(nameof(CreateInPlace), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(
                    marker.Component.Type,
                    typeof(VisualElement)
                );

            currentLevelInstance = (ViewContainerRef)method.Invoke(
                null,
                new object[]
                {
                    marker.parent,
                    new Injector(closestUpperLevelInstance!.anchor.Injector)
                }
            );
            
            Debug.Log($"[ViewContainerRef (HandleScaffolded):] Created NEW ViewContainerRef ({currentLevelInstance}). Its anchor is ({currentLevelInstance.anchor.VisualElement.GetType().Name} {currentLevelInstance.anchor.VisualElement.name}). It was made based on element ({marker.GetType().Name} {marker.name}), with parent ({marker.parent.GetType().Name} {marker.parent.name})");
        }

        private static ElementRef CreateAndInstantiateElementRefFromVisualTree<TComp>(
            VisualElement parent,
            Injector injector
        )
            where TComp : Component, new()
        {
            var metadata = injector.Resolve<ComponentMetadata<TComp>>(typeof(ComponentMetadata<TComp>));
            var element = metadata.UXML.Instantiate();
            parent.Add(element);

            return new ElementRef(element, injector);
        }

        protected override void OnInjected() { }

        private class Provider : ValueProvider<ViewContainerRef>
        {
            public Provider(ViewContainerRef viewContainerRef)
            {
                Value = viewContainerRef;
                IsTyped = true;
                TypeToken = typeof(ViewContainerRef);
            }
        }
    }
}