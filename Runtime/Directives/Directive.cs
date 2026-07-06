using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Armoury.UI.Injectors;
using UnityEngine.UIElements;

namespace Armoury.UI
{
    public abstract class Directive : Injectable, INotifyBindablePropertyChanged
    {
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
        
        protected bool Set<T>(
            ref T field,
            T value,
            [CallerMemberName] string propertyName = ""
        )
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            NotifyChange(propertyName);

            return true;
        }
        
        protected Directive(Injector injector) : base(injector) { }
        
        
        private void NotifyChange([CallerMemberName] string propertyName = "")
        {
            propertyChanged?.Invoke(
                this,
                new BindablePropertyChangedEventArgs(propertyName)
            );
        }
        
        protected virtual void OnInputsChanged(InputChangeMask changed) { }

        internal void __UniNgOnInputsChanged(InputChangeMask changed)
        {
            OnInputsChanged(changed);
        }
    }

    public class DirectiveRef<TDirective> : Injectable where TDirective : Directive
    {
        public readonly ElementRef Anchor;
        public readonly TDirective Instance;
        
        public DirectiveRef(TDirective directive, ElementRef anchor, Injector injector = null) : base(injector)
        {
            Anchor = anchor;
            Instance = directive;
        }
        
        // public void Destroy();
        // public event Action OnDestroy
        
        protected override void OnInjected() { }
        
        public class Provider : ValueProvider<DirectiveRef<TDirective>>
        {
            public Provider(DirectiveRef<TDirective> directiveRef)
            {
                Value = directiveRef;
                IsTyped = true;
                TypeToken = typeof(DirectiveRef<TDirective>);
            }
        }
    }
}