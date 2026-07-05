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

        public static ViewContainerRef CreateViewContainerRefInPlace<TComp, TElement>(
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
        
        public static ViewContainerRef CreateChildViewContainerRef<TComp, TElement>(
            VisualElement parent,
            Injector injector,
            int? index = null
        )
            where TComp : Component, new()
            where TElement : VisualElement
        {
            // Create an Injector for the new ViewContainerRef as a child to the specified injector
            var viewContainerInjector = new Injector(injector);
            
            // Create an Injector for the ElementRef as a child to the newly-created ViewContainerRef injector
            var elementInjector = new Injector(viewContainerInjector);
            
            var elementRef = CreateAndInstantiateElementRefFromVisualTree<TComp>(parent, elementInjector, index);
            var viewContainerRef = new ViewContainerRef(elementRef, viewContainerInjector);
            
            viewContainerInjector.Inject(new Provider(viewContainerRef));

            viewContainerRef.CreateComponent<TComp, TElement>(elementRef, index);
            
            return viewContainerRef;
        }
        
        private static readonly System.Reflection.MethodInfo CreateChildMethod =
            typeof(ViewContainerRef)
                .GetMethod(
                    nameof(CreateChildViewContainerRef),
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                )!;

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
                CreateAndInstantiateElementRefFromVisualTree<TComp>(anchor.VisualElement.parent, new Injector(Injector), index), 
                index: index
            );
        }
        
        private static readonly System.Reflection.MethodInfo InstanceCreateComponentMethod =
            typeof(ViewContainerRef)
                .GetMethod(
                    nameof(CreateComponent),
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                );
        
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

                if (Marker.Is<NgIfDirectiveMarker>(child, out var ngIfMarker))
                {
                    ngIfMarker.Unwrap();

                    i++;

                    continue;
                }

                if (Marker.Is<NgForDirectiveMarker>(child, out var ngForMarker))
                {
                    ngForMarker.Compile();

                    i++;

                    continue;
                }

                if (Marker.Is<ComponentMarker>(child, out var componentMarker))
                {
                    HandleMarker(
                        marker: componentMarker,
                        currentLevelInstance: ref currentLevelInstance,
                        closestUpperLevelInstance: closestUpperLevelInstance
                    );

                    i++;

                    continue;
                }

                ScaffoldChildren(
                    parent: child,
                    currentLevelInstance: ref childLevelInstance,
                    closestUpperLevelInstance: currentLevelInstance ?? closestUpperLevelInstance
                );

                i++;
            }
        }
        
        private static void HandleMarker(
            ComponentMarker marker,
            ref ViewContainerRef currentLevelInstance,
            ViewContainerRef closestUpperLevelInstance
        )
        {
            var componentType = marker.ComponentType;
            if (componentType == null)
            {
                Debug.LogError(
                    $"[ViewContainerRef (HandleScaffolded):] Cannot scaffold marker ({marker.GetType().Name} {marker.name}) because its ComponentType is null."
                );

                return;
            }

            var parent = marker.parent;
            var markerIndexInParent = parent.IndexOf(marker);
            
            // TODO: Dispose of any memory owned by the component marker
            marker.RemoveFromHierarchy();

            if (currentLevelInstance != null)
            {
                Debug.Log($"[ViewContainerRef (HandleScaffolded):] Found EXISTING ViewContainerRef ({currentLevelInstance}). Its anchor is ({currentLevelInstance.anchor.VisualElement.GetType().Name} {currentLevelInstance.anchor.VisualElement.name}).");

                InstanceCreateComponentMethod
                    .MakeGenericMethod(componentType, typeof(VisualElement))
                    .Invoke(currentLevelInstance, new object[] { markerIndexInParent });

                // Debug.Log($"[ViewContainerRef (HandleScaffolded):] Made NEW component for EXISTING ViewContainerRef ({currentLevelInstance}). The ViewContainerRef anchor is ({currentLevelInstance.anchor.VisualElement.GetType().Name} {currentLevelInstance.anchor.VisualElement.name}). The ElementRef's element is ({elementRef.VisualElement.GetType().Name} {elementRef.VisualElement.name})");
                // Debug.Log($"[ViewContainerRef (HandleScaffolded):] The <NEW component for EXISTING ViewContainerRef>'s Injector lists as follows: {string.Join(",", elementRef.Injector.Providers.Select(prov => $"[{prov.GetType().Name}:] {prov.StringToken}/{prov.TypeToken}"))}");

                Debug.Log(
                    $"[ViewContainerRef (HandleScaffolded):] Made NEW component for EXISTING ViewContainerRef ({currentLevelInstance}). The ViewContainerRef anchor is ({currentLevelInstance.anchor.VisualElement.GetType().Name} {currentLevelInstance.anchor.VisualElement.name})");
                
                return;
            }
            
            currentLevelInstance = (ViewContainerRef)CreateChildMethod
                .MakeGenericMethod(componentType, typeof(VisualElement))
                .Invoke(
                    null,
                    new object[]
                    {
                        parent,
                        closestUpperLevelInstance.anchor.Injector,
                        markerIndexInParent
                    }
                );

            Debug.Log($"[ViewContainerRef (HandleScaffolded):] Created NEW ViewContainerRef ({currentLevelInstance}). Its anchor is ({currentLevelInstance.anchor.VisualElement.GetType().Name} {currentLevelInstance.anchor.VisualElement.name}). It was made based on element ({marker.GetType().Name} {marker.name})");
        }

        private static ElementRef CreateAndInstantiateElementRefFromVisualTree<TComp>(
            VisualElement parent,
            Injector injector,
            int? index = null
        )
            where TComp : Component, new()
        {
            var metadata = injector.Resolve<ComponentMetadata<TComp>>(typeof(ComponentMetadata<TComp>));
            var element = metadata.UXML.Instantiate();
            element.name = typeof(TComp).Name;

            if (index != null)
            {
                parent.Insert(index.Value, element);   
            }
            else
            {
                parent.Add(element);
            }

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