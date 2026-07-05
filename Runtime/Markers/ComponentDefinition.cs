using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Armoury.UI.Markers
{
    [System.Serializable]
    [UxmlObject]
    public partial class ComponentDefinition
    {
        [UxmlObjectReference("uning-component-inputs")]
        public List<InputBase> Inputs { get; set; }
    }
}