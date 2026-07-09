using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Armoury.UI.Injectors
{
    [CreateAssetMenu(fileName = "My Environment Injector", menuName = "UniNg/Injector/Environment Injector Definition")]
    public class EnvironmentInjectorDefinition : SerializedScriptableObject, IProviderCollection
    {
        [BoxGroup("Providers"), HideLabel, ListDrawerSettings(DefaultExpandedState = true), OdinSerialize]
        public List<IProviderCollection> Providers = new();

        public void InjectInto(Injector injector)
        {
            if (Providers == null)
                return;

            for (var i = 0; i < Providers.Count; i++)
            {
                var provider = Providers[i];

                if (provider == null)
                    continue;
                
                provider.InjectInto(injector);
            }
        }
        
        public IEnumerable<Provider> Get()
        {
            if (Providers == null)
                yield break;

            for (var i = 0; i < Providers.Count; i++)
            {
                var provider = Providers[i];

                if (provider == null)
                    continue;

                foreach (var result in provider.Get())
                    yield return result;
            }
        }
        
        public void GetNonAlloc(List<Provider> results)
        {
            if (Providers == null)
                return;

            for (var i = 0; i < Providers.Count; i++)
            {
                var provider = Providers[i];

                if (provider == null)
                    continue;

                provider.GetNonAlloc(results);
            }
        }
    }
}
