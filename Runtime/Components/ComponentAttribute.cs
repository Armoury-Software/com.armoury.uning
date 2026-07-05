using System;

namespace Armoury.UI
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ComponentAttribute : Attribute
    {
        public string TemplatePath { get; set; }

        public ComponentAttribute()
        {
        }

        public ComponentAttribute(string templatePath)
        {
            TemplatePath = templatePath;
        }
    }
}