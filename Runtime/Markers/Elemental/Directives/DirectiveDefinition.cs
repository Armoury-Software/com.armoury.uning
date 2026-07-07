using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Armoury.UI.Markers
{
    [UxmlObject, System.Serializable]
    public partial class DirectiveDefinition
    {
        [UxmlAttribute("type")]
        public System.Type Type;
        
        [UxmlObjectReference("inputs-legacy")]
        public InputBinding[] Inputs_Legacy { get; set; }

        [UxmlObjectReference("inputs")] public List<InputBinding> Inputs { get; set; }

        // [UxmlAttribute("input-count")]
        // public int InputCount { get; set; }

        // [System.NonSerialized] internal InputAssignment[] CompiledInputs;
        // [System.NonSerialized] internal int CompiledInputCounts;
        // [System.NonSerialized] internal bool IsDirty = true;
    }
}