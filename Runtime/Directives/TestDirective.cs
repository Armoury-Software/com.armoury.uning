using Unity.Properties;
using Armoury.UI.Injectors;
using Armoury.UI.Inputs;
using UnityEngine;

namespace Armoury.UI
{
    public class TestDirective : Directive
    {
        [CreateProperty, Input] public string Test = "Test Val";
        [CreateProperty, Input] public int TestInt = 24;
        [CreateProperty, Input] public float TestFloat = 1.5f;
        [CreateProperty, Input] public UnityEngine.Object TestObject;
        [CreateProperty, Input] public ScriptableObject TestScriptableObject;
        
        public TestDirective(Injector injector) : base(injector) { }
        
        protected override void OnInjected() { }
    }
}