using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Armoury.UI.Markers.Elemental
{
    [UxmlElement]
    public partial class ElementMarker : Marker
    {
        public ElementMarker()
        {
            style.display = DisplayStyle.None;
        }
        
        [UxmlObjectReference("arm-directives")]
        public List<DirectiveDefinition> Directives { get; set; }
    }
}