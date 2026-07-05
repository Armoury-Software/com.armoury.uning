using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Armoury.UI.Markers.Elemental
{
    [UxmlElement]
    public partial class ElementMarker : Marker
    {
        [UxmlObjectReference("uning-directives")]
        public List<DirectiveDefinition> Directives { get; set; }
    }
}