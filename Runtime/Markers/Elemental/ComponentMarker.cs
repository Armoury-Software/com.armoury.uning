using UnityEngine.UIElements;

namespace Armoury.UI.Markers.Elemental
{
    [UxmlElement]
    public partial class ComponentMarker : ElementMarker
    {
        [UxmlObjectReference("arm-component")]
        public ComponentDefinition Component { get; set; }
    }
}