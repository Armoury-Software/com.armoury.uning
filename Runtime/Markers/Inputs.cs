using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Armoury.UI.Markers
{
    /*[System.Serializable]
    [UxmlObject]
    public partial class Input<TDirective, TType> : InputBase where TDirective : Directive
    {
        [UxmlAttribute("uning-input-name")] public string Name { get; set; }
        
        public Input()
        {
            Debug.LogWarning($"Instantiated an Input ({GetType().Name}) with no constructor parameters");
        }

        public Input(string name, TType value)
        {
            
        }
    }*/

    [System.Serializable]
    [UxmlObject]
    public abstract partial class InputBase
    {
        [UxmlAttribute("uning-input-name")]
        public string Name { get; set; }

        [UxmlAttribute("uning-input-field-type")]
        public string FieldTypeName { get; set; }
    }

    [System.Serializable]
    [UxmlObject]
    public partial class StringInput : InputBase
    {
        [UxmlAttribute("uning-input-value")]
        public string Value { get; set; }
    }

    [System.Serializable]
    [UxmlObject]
    public partial class IntInput : InputBase
    {
        [UxmlAttribute("uning-input-value")]
        public int Value { get; set; }
    }

    [System.Serializable]
    [UxmlObject]
    public partial class FloatInput : InputBase
    {
        [UxmlAttribute("uning-input-value")]
        public float Value { get; set; }
    }

    [System.Serializable]
    [UxmlObject]
    public partial class BoolInput : InputBase
    {
        [UxmlAttribute("uning-input-value")]
        public bool Value { get; set; }
    }

    [System.Serializable]
    [UxmlObject]
    public partial class ObjectInput : InputBase
    {
        [UxmlAttribute("uning-input-value")]
        public UnityEngine.Object Value { get; set; }
    }
}