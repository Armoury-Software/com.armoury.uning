using UnityEngine.UIElements;

namespace Armoury.UI.Markers.Structural
{
    [UxmlElement]
    public abstract partial class StructuralDirectiveMarker : Marker
    {
        protected StructuralDirectiveMarker()
        {
            style.flexGrow = 0;
            style.flexShrink = 0;
        }
    }
}