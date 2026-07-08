using System;

namespace Armoury.UI
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class InputAttribute : BindingAttribute
    {
        public InputAttribute(string displayName = null) : base(displayName) { }
    }
}