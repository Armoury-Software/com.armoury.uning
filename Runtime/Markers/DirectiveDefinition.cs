using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Armoury.UI.Markers
{
    [System.Serializable]
    [UxmlObject]
    public partial class DirectiveDefinition
    {
        private System.Type _type;

        [UxmlAttribute("uning-directive-type")]
        [UxmlTypeReference(typeof(Directive))]
        public System.Type Type
        {
            get => _type;
            set
            {
                if (
                    value != null &&
                    (
                        typeof(Component).IsAssignableFrom(value)/* ||
                        typeof(StructuralDirectiveMarker).IsAssignableFrom(value)*/
                    )
                )
                {
                    Debug.LogError(
                        $"'{value.FullName}' is not allowed here. Only attribute directives can be used."
                    );

                    _type = null;
                    return;
                }

                _type = value;
            }
        }

        [UxmlObjectReference("uning-directive-inputs")]
        public List<InputBase> Inputs { get; set; }
    }
}