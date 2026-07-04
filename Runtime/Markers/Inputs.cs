using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Armoury.UI.Markers
{
    /*[System.Serializable]
    [UxmlObject]
    public partial class Input<TDirective, TType> : InputBase where TDirective : Directive
    {
        [UxmlAttribute("arm-input-name")] public string Name { get; set; }
        
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
        [UxmlAttribute("arm-input-name")]
        public string Name { get; set; }

        [UxmlAttribute("arm-input-field-type")]
        public string FieldTypeName { get; set; }
    }

    [System.Serializable]
    [UxmlObject]
    public partial class StringInput : InputBase
    {
        [UxmlAttribute("arm-value")]
        public string Value { get; set; }
    }

    [System.Serializable]
    [UxmlObject]
    public partial class IntInput : InputBase
    {
        [UxmlAttribute("arm-value")]
        public int Value { get; set; }
    }

    [System.Serializable]
    [UxmlObject]
    public partial class FloatInput : InputBase
    {
        [UxmlAttribute("arm-value")]
        public float Value { get; set; }
    }

    [System.Serializable]
    [UxmlObject]
    public partial class BoolInput : InputBase
    {
        [UxmlAttribute("arm-value")]
        public bool Value { get; set; }
    }

    [System.Serializable]
    [UxmlObject]
    public partial class ObjectInput : InputBase
    {
        [UxmlAttribute("arm-value")]
        public UnityEngine.Object Value { get; set; }
    }
}