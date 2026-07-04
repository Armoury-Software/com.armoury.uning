using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Armoury.UI.Injectors
{
    public abstract class Injectable
    {
        private Injector _injector;
        public Injector Injector
        {
            get => _injector;
            internal set
            {
                var prev = _injector;
                _injector = value;
                
                if (prev == null && value != null)
                {
                    OnInjected();
                }
            }
        }

        public Injectable(Injector injector)
        {
            Injector = injector;
        }

        protected abstract void OnInjected(); // TODO: Replace with "IOnInit" interface, if adds no memory allocation
    }
    
    public class Injector
    {
        internal readonly Injector parent;

        private readonly Dictionary<string, Provider> _stringTokenProviders = new();
        private readonly Dictionary<System.Type, Provider> _typeTokenProviders = new();
        
        public IEnumerable<Provider> Providers
        {
            get
            {
                foreach (var provider in _stringTokenProviders.Values)
                    yield return provider;

                foreach (var provider in _typeTokenProviders.Values)
                    yield return provider;
            }
        }

        internal Injector(Injector parent)
        {
            if (parent != null)
            {
                this.parent = parent;
            }
        }

        internal Injector() : this(null) { }

        public void Inject(Provider provider)
        {
            switch (provider.IsTyped)
            {
                case true when provider.TypeToken != null:
                    _typeTokenProviders[provider.TypeToken] = provider;
                    break;
                case true when !string.IsNullOrEmpty(provider.StringToken):
                    _stringTokenProviders[provider.StringToken] = provider;
                    break;
                default:
                    Debug.LogError("Can't find a proper token value for injection");
                    break;
            }
        }

        public T Resolve<T>(string tokenId)
        {
            var ancestor = this;
            while (ancestor != null)
            {
                if (!ancestor._stringTokenProviders.TryGetValue(tokenId, out var provider))
                {
                    ancestor = ancestor.parent;
                    continue;
                }

                return provider.Resolve<T>(ancestor);
            }

            throw new System.Exception($"Could not injector-resolve for ({tokenId})");
        }

        public T Resolve<T>(System.Type type)
        {
            var ancestor = this;
            while (ancestor != null)
            {
                if (!ancestor._typeTokenProviders.TryGetValue(type, out var provider))
                {
                    ancestor = ancestor.parent;
                    
                    continue;
                }

                return provider.Resolve<T>(ancestor);
            }

            throw new System.Exception($"Could not injector-resolve for ({type})");
        }
    }

    [System.Serializable] public sealed class StringProvider : ValueProvider<string> { }
    [System.Serializable] public sealed class IntegerProvider : ValueProvider<int> { }
    [System.Serializable] public sealed class FloatProvider : ValueProvider<float> { }
    [System.Serializable] public sealed class ObjectProvider : ValueProvider<Object> { }

    [System.Serializable]
    public abstract class ValueProvider<TValue> : Provider
    {
        [field: SerializeField] public TValue Value { get; set; }

        public override T Resolve<T>(Injector injector)
        {
            if (Value is T val)
            {
                return val;
            }
            
            throw new System.Exception($"Could not provider-resolve for ({typeof(T)})");
        }
    }

    [System.Serializable]
    public abstract class Provider : IProviderCollection
    {
        [field: SerializeField, OnValueChanged(nameof(OnIsTypedChanged))]
        public bool IsTyped { get; set; }

        [field: SerializeField, HideIf(nameof(IsTyped))]
        public string StringToken { get; set; }
        
        [field: OdinSerialize, ShowIf(nameof(IsTyped))]
        public System.Type TypeToken { get; set; }

        public abstract T Resolve<T>(Injector injector);

        private void OnIsTypedChanged()
        {
            if (!IsTyped)
            {
                TypeToken = null;
            }
            else
            {
                StringToken = null;
            }
        }

        public IEnumerable<Provider> Get()
        {
            yield return this;
        }
        
        public virtual void GetNonAlloc(List<Provider> results)
        {
            results.Add(this);
        }
        
        public virtual void InjectInto(Injector injector)
        {
            injector.Inject(this);
        }
    }

    public interface IProviderCollection
    {
        void InjectInto(Injector injector);
        IEnumerable<Provider> Get();
        void GetNonAlloc(List<Provider> results);
    }
}