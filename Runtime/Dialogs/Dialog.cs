using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Armoury.UI.Injectors;

namespace Armoury.UI.Dialog
{
    public class DialogRef : Injectable
    {
        public DialogRef(Injector injector) : base(injector) { }

        public void Close()
        {
            Injector.Resolve<DialogService>(typeof(DialogService)).CloseLatest();
        }
        
        public class Provider : ValueProvider<DialogRef>
        {
            public Provider(DialogRef dialogRef)
            {
                Value = dialogRef;
                IsTyped = true;
                TypeToken = typeof(DialogRef);
            }
        }
        
        protected override void OnInjected() { }
    }
    
    // TODO: The DialogService currently creates ViewContainerRefs, but doesn't destroy them.
    public class DialogService : Injectable
    {
        private readonly ElementRef _root;
        private VisualElement _host;

        private readonly Stack<DialogRef> _dialogs = new();
        
        public DialogService(Injector injector) : base(injector)
        {
            _root = injector.Resolve<ElementRef>(typeof(ElementRef));
        }

        public void Open<TComp>(
            TComp component,
            Length width = default, Length minWidth = default, Length maxWidth = default,
            Length height = default, Length minHeight = default, Length maxHeight = default,
            string panelClass = "", bool disableCloseOnBackdropClick = false
        )
            where TComp : Component, new()
        {
            if (!HostExists())
            {
                CreateHost();
            }

            Push(
                component,
                width: width, minWidth: minWidth, maxWidth: maxWidth,
                height: height, minHeight: minHeight, maxHeight: maxHeight,
                panelClass: panelClass, disableCloseOnBackdropClick: disableCloseOnBackdropClick
            );
        }

        private void Push<TComp>(
            TComp component,
            Length width = default, Length minWidth = default, Length maxWidth = default,
            Length height = default, Length minHeight = default, Length maxHeight = default,
            string panelClass = "", bool disableCloseOnBackdropClick = false
        )
            where TComp : Component, new()
        {
            var dialog = new VisualElement { name = $"Dialog #{_dialogs.Count} ({component.GetType().Name})" };
            _host!.Add(dialog);
            dialog.AddToClassList("cdk__dialog-container__dialog");
            dialog.StretchToParentSize();

            var backdrop = new VisualElement { name = $"(Backdrop) {dialog.name}" };
            dialog.Add(backdrop);
            backdrop.AddToClassList("cdk__dialog-container__dialog__backdrop");
            backdrop.style.position = Position.Absolute;
            backdrop.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.2f));
            backdrop.StretchToParentSize();

            if (!disableCloseOnBackdropClick)
            {
                backdrop.RegisterCallbackOnce<ClickEvent>(OnBackdropClick);
            }

            var innerDialog = new VisualElement { name = $"(Inner) {dialog.name}" };
            dialog.Add(innerDialog);
            innerDialog.AddToClassList("cdk__dialog-container__dialog__inner");
            
            // Align on center X/Y
            innerDialog.style.position = Position.Absolute;
            innerDialog.style.left = Length.Percent(50);
            innerDialog.style.top = Length.Percent(50);
            innerDialog.style.translate = new Translate(
                Length.Percent(-50),
                Length.Percent(-50)
            );
            
            // TODO: Take default values from a provider
            innerDialog.style.height = height == default ? 200 : height;

            if (width != default)
            {
                innerDialog.style.width = width;
            }
            
            innerDialog.style.minWidth = minWidth == default ? 300 : minWidth;
            innerDialog.style.maxWidth = maxWidth == default ? new Length(100, LengthUnit.Percent) : maxWidth;
            
            if (height != default)
            {
                innerDialog.style.height = height;
            }
            
            innerDialog.style.minHeight = minHeight == default ? 300 : minHeight;
            innerDialog.style.maxHeight = maxHeight == default ? new Length(100, LengthUnit.Percent) : maxHeight;

            if (!string.IsNullOrEmpty(panelClass))
            {
                innerDialog.AddToClassList(panelClass);
            }

            var innerInjector = new Injector(Injector);
            var dialogRef = new DialogRef(innerInjector);
            
            innerInjector.Inject(new DialogRef.Provider(dialogRef));
            _dialogs.Push(dialogRef);

            ViewContainerRef.CreateChild<
                TComp,
                VisualElement /* TODO: Use real VisualElement type, taken from metadata (this will need usage of Reflection)*/
            >(
                innerDialog, innerInjector
            );
        }

        public void CloseLatest()
        {
            // TODO: Remove ViewContainerRef and all its children ViewContainerRef's/Injectors
            var dialog = _dialogs.Pop();
            // TODO: Dispose of dialog
            // TODO: Dispose of the inner injector

            var element = _host.ElementAt(_dialogs.Count);
            element.RemoveFromHierarchy();

            if (_dialogs.Count == 0)
            {
                DestroyHost();
            }
        }

        private void OnBackdropClick(ClickEvent evt) => CloseLatest();

        private void CreateHost()
        {
            _root.VisualElement.Add(_host = new VisualElement { name = "Dialog Container" });
            _host.AddToClassList("cdk__dialog-container");
            _host.style.position = Position.Absolute;
            _host.StretchToParentSize();
        }

        private void DestroyHost()
        {
            // TODO: Remove ViewContainerRef and all its children ViewContainerRef's/Injectors
            _host.RemoveFromHierarchy();
            _host = null;
        }

        private bool HostExists() => _host != null;
        
        protected override void OnInjected() { }
        
        // ReSharper disable once UnusedType.Global
        public class Provider : Armoury.UI.Injectors.Provider
        {
            private DialogService _service;

            public Provider()
            {
                IsTyped = true;
                TypeToken = typeof(DialogService);
            }
            
            public override T Resolve<T>(Injector injector)
            {
                if (typeof(T) == typeof(DialogService))
                {
                    _service ??= new DialogService(injector);
                    
                    if (_service is T service)
                    {
                        return service;
                    }
                }
                
                throw new System.Exception($"Could not provider-resolve for ({typeof(T)})");
            }
        }
    }
}