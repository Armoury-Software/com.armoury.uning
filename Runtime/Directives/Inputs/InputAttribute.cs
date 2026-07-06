using System;

namespace Armoury.UI
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public sealed class InputAttribute : Attribute
    {
        public string DisplayName { get; }

        public InputAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }
    }
}