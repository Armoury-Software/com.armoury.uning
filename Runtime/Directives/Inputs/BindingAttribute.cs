using System;

namespace Armoury.UI
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class BindingAttribute : Attribute
    {
        public string DisplayName { get; }

        public BindingAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }
    }
}