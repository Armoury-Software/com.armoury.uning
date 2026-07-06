using UnityEngine.UIElements;

namespace Armoury.UI.Markers.Elemental
{
    [UxmlElement]
    public partial class ElementMarker : Marker
    {
        [UxmlObjectReference("directives")]
        public DirectivesCollection Directives { get; set; }
    }
}