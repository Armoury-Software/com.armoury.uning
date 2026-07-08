using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Armoury.UI.Markers
{
    [UxmlObject, System.Serializable]
    public partial class DirectiveDefinition
    {
        [UxmlAttribute("type")]
        public System.Type Type;
        
        [UxmlAttribute("parent-type"), UxmlTypeReference(typeof(Component))]
        public System.Type ParentType;
        
        [UxmlObjectReference("inputs")]
        public List<InputBinding> Inputs { get; set; }
    }
}