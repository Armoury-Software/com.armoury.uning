using UnityEngine.UIElements;

namespace Armoury.UI.Markers
{
    [UxmlElement]
    public abstract partial class Marker : VisualElement
    {
        public bool IsCompiled { get; protected set; }

        protected Marker()
        {
            pickingMode = PickingMode.Ignore;
            // dataSourceType = GetType();
            name = string.IsNullOrEmpty(name) ? GetType().Name : name;
        }
        
        public static bool Is<TMarker>(VisualElement element, out TMarker marker)
            where TMarker : Marker
        {
            if (element is TMarker tMarker)
            {
                marker = tMarker;
                return true;
            }

            marker = null;
            return false;
        }

        public static bool Is<TMarker>(VisualElement element) where TMarker : Marker => element is TMarker;
        public static bool Is(VisualElement element) => Is<Marker>(element);
    }
}
