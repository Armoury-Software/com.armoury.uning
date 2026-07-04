using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Armoury.UI.Markers
{
    [System.Serializable]
    [UxmlObject]
    public partial class ComponentDefinition
    {
        [UxmlAttribute("arm-component-type")]
        [UxmlTypeReference(typeof(Component))]
        public System.Type Type { get; set; }

        [UxmlObjectReference("arm-component-inputs")]
        public List<InputBase> Inputs { get; set; }
    }
}