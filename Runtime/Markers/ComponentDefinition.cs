using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Armoury.UI.Markers
{
    [System.Serializable]
    [UxmlObject]
    public partial class DirectivesCollection
    {
        [UxmlObjectReference("collection")] public List<DirectiveDefinition> Directives { get; set; }
    }

    [System.Serializable]
    [UxmlObject]
    public partial class InputBinding
    {
        public const int CollectionUnspecified = -1, CollectionDynamic = -2;
        
        [UxmlAttribute("enabled")]
        public bool Enabled { get; set; }

        [UxmlAttribute("input-id")]
        public ulong InputId { get; set; }

        [UxmlAttribute("input-index")]
        public int InputIndex { get; set; }

        [UxmlAttribute("input-name")]
        public string InputName { get; set; }

        [UxmlAttribute("input-alias")]
        public string InputAlias { get; set; }

        [UxmlAttribute("source")]
        public InputValueSource Source { get; set; }

        [UxmlAttribute("parent-binding-id")]
        public ulong ParentBindingId { get; set; }

        [UxmlAttribute("collection-index")]
        public int CollectionIndex { get; set; } = CollectionUnspecified;
        public bool UsesCollectionElement => CollectionIndex == CollectionDynamic || CollectionIndex >= 0;

        [UxmlAttribute("kind")]
        public InputValueKind Kind { get; set; }

        [UxmlObjectReference("literal-value")]
        public InputValueDefinition LiteralValue { get; set; }
        
        // Editor/debugging (maybe wrap in #if UNITY_EDITOR?)
        [UxmlAttribute("binding-path")]
        public string BindingPath { get; set; }
        
        public bool IsMissing;
        public string ValueTypeName;
        
        public static InputBinding CreateBinding(in InputDescriptor descriptor)
        {
            return new InputBinding
            {
                InputId = descriptor.Id,
                InputIndex = descriptor.Index,
                InputName = descriptor.MemberName,
                InputAlias = descriptor.Alias,
                Kind = descriptor.Kind,
                Source = InputValueSource.Literal,
                LiteralValue = InputValueDefinition.Create(descriptor)
            };
        }

        public override string ToString()
        {
            return $"{GetType().Name}(Name={InputName}, Alias={InputAlias}, Kind={Kind}, Index={InputIndex}, Id={InputId})";
        }
    }
    
    /*[System.Serializable]
    public sealed class ComponentInputBinding
    {
        public ulong InputId;
        public int InputIndex;

        public string InputName;
        public string InputAlias;

        public ComponentInputValueSource Source;

        public InputValueDefinition LiteralValue;

        // Editor/debugging (maybe wrap in #if UNITY_EDITOR?)
        public string BindingPath;
        public bool IsMissing;
        public string ValueTypeName;

        public ulong ParentBindingId;
        public InputValueKind Kind;

        public static ComponentInputBinding CreateBinding(in InputDescriptor descriptor)
        {
            return new ComponentInputBinding
            {
                InputId = descriptor.Id,
                InputIndex = descriptor.Index,
                InputName = descriptor.MemberName,
                InputAlias = descriptor.Alias,
                Kind = descriptor.Kind,
                Source = ComponentInputValueSource.Literal,
                LiteralValue = InputValueDefinition.Create(descriptor)
            };
        }
    }*/

    public enum InputValueSource
    {
        Literal,
        ParentBinding
    }

    [System.Serializable]
    [UxmlObject]
    public abstract partial class InputValueDefinition
    {
        public abstract InputValue ToInputValue();

        public static InputValueDefinition Create(in InputDescriptor descriptor)
            => descriptor.Kind switch {
                InputValueKind.Bool => new BoolInputValueDefinition(),
                InputValueKind.Int => new IntInputValueDefinition(),
                InputValueKind.Float => new FloatInputValueDefinition(),
                InputValueKind.Double => new DoubleInputValueDefinition(),
                InputValueKind.String => new StringInputValueDefinition(),
                InputValueKind.Object => new ObjectInputValueDefinition(),
                InputValueKind.Value => new ValueInputValueDefinition(),
                _ => null
            };
    }
    
    [System.Serializable]
    [UxmlObject]
    public sealed partial class BoolInputValueDefinition : InputValueDefinition
    {
        [UxmlAttribute("value")]
        public bool Value { get; set; }

        public override InputValue ToInputValue()
        {
            return InputValue.FromBool(Value);
        }
    }
    
    [System.Serializable]
    [UxmlObject]
    public sealed partial class IntInputValueDefinition : InputValueDefinition
    {
        [UxmlAttribute("value")]
        public int Value { get; set; }

        public override InputValue ToInputValue()
        {
            return InputValue.FromInt(Value);
        }
    }
    
    [System.Serializable]
    [UxmlObject]
    public sealed partial class FloatInputValueDefinition : InputValueDefinition
    {
        [UxmlAttribute("value")]
        public float Value { get; set; }

        public override InputValue ToInputValue()
        {
            return InputValue.FromFloat(Value);
        }
    }
    
    [System.Serializable]
    [UxmlObject]
    public sealed partial class DoubleInputValueDefinition : InputValueDefinition
    {
        [UxmlAttribute("value")]
        public double Value { get; set; }

        public override InputValue ToInputValue()
        {
            return InputValue.FromDouble(Value);
        }
    }
    
    [System.Serializable]
    [UxmlObject]
    public sealed partial class StringInputValueDefinition : InputValueDefinition
    {
        [UxmlAttribute("value"), CreateProperty]
        public string Value { get; set; }

        public override InputValue ToInputValue()
        {
            return InputValue.FromString(Value);
        }
    }
    
    [System.Serializable]
    [UxmlObject]
    public sealed partial class ObjectInputValueDefinition : InputValueDefinition
    {
        [UxmlAttribute("value")]
        public UnityEngine.Object Value { get; set; }

        public override InputValue ToInputValue()
        {
            return InputValue.FromObject(Value);
        }
    }
    
    [System.Serializable]
    [UxmlObject]
    public sealed partial class ValueInputValueDefinition : InputValueDefinition
    {
        // [UxmlAttribute("value")]
        public object Value { get; set; }

        public override InputValue ToInputValue()
        {
            return InputValue.FromValue(Value);
        }
    }
    
    public static class ComponentInputCompiler
    {
        public static int Compile(
            ComponentDefinition definition,
            object parentDataSource,
            InputAssignment[] output
        )
        {
            if (definition?.Inputs == null)
               return 0;

            var count = 0;

            for (var i = 0; i < definition.Inputs.Count; i++)
            {
                var binding = definition.Inputs[i];
                if (!binding.Enabled) continue;

                InputValue value;

                if (binding.Source == InputValueSource.Literal)
                {
                    value = binding.LiteralValue.ToInputValue();
                }
                else
                {
                    value = ParentBindingResolver.Resolve(
                        parentDataSource,
                        in binding
                    );
                }

                output[count++] = new InputAssignment(
                    binding.InputId,
                    in value
                );
            }

            return count;
        }
    }
    
    public readonly struct InputDescriptor
    {
        public readonly ulong Id;
        public readonly int Index;

        public readonly string MemberName;
        public readonly string Alias;

        public readonly System.Type ValueType;
        public readonly InputValueKind Kind;

        public string DisplayName => string.IsNullOrEmpty(Alias)
            ? MemberName
            : Alias;

        public InputDescriptor(
            ulong id,
            int index,
            string memberName,
            string alias,
            System.Type valueType,
            InputValueKind kind
        )
        {
            Id = id;
            Index = index;
            MemberName = memberName;
            Alias = alias;
            ValueType = valueType;
            Kind = kind;
        }
    }
    
    public static class ComponentInputRegistry
    {
        private static readonly Dictionary<System.Type, InputDescriptor[]> InputsByType = new();

        private static readonly InputDescriptor[] Empty = System.Array.Empty<InputDescriptor>();

        public static void Register(
            System.Type componentType,
            InputDescriptor[] descriptors
        )
        {
            if (componentType == null)
                throw new System.ArgumentNullException(nameof(componentType));

            InputsByType[componentType] = descriptors ?? Empty;
        }

        public static InputDescriptor[] Get(System.Type componentType)
        {
            if (componentType == null)
                return Empty;

            return InputsByType.TryGetValue(componentType, out var descriptors)
                ? descriptors
                : Empty;
        }

        public static bool TryGet(
            System.Type componentType,
            ulong inputId,
            out InputDescriptor descriptor
        )
        {
            var descriptors = Get(componentType);

            for (var i = 0; i < descriptors.Length; i++)
            {
                if (descriptors[i].Id == inputId)
                {
                    descriptor = descriptors[i];
                    return true;
                }
            }

            descriptor = default;
            return false;
        }
    }

    public interface IParentBindingSource
    {
        bool TryResolveParentBinding(
            ulong bindingId,
            out InputValue value);

        bool TryResolveParentBindingElement(
            ulong bindingId,
            int index,
            out InputValue value);
    }
    
    public static class ParentBindingResolver
    {
        public static InputValue Resolve(
            object parent,
            in InputBinding binding)
        {
            if (TryResolve(parent, in binding, out var value))
                return value;
            
            var bindingPath = binding.UsesCollectionElement
                ? $"{binding.BindingPath}[{(binding.CollectionIndex == InputBinding.CollectionDynamic ? "Dynamic" : binding.CollectionIndex)}]"
                : binding.BindingPath;

            throw new System.InvalidOperationException(
                $"Could not resolve parent binding '{bindingPath}' " +
                $"with id '{binding.ParentBindingId}' on parent " +
                $"'{parent?.GetType().FullName ?? "null"}'.");
        }

        public static bool TryResolve(
            object parent,
            in InputBinding binding,
            out InputValue value)
        {
            value = default;

            if (parent is not IParentBindingSource source)
            {
                return false;
            }

            if (binding.UsesCollectionElement)
            {
                return source.TryResolveParentBindingElement(
                    binding.ParentBindingId,
                    binding.CollectionIndex,
                    out value);
            }

            return source.TryResolveParentBinding(
                binding.ParentBindingId,
                out value);
        }
    }

    public readonly struct ParentBindingDescriptor
    {
        public readonly ulong Id;
        public readonly string Path;
        public readonly System.Type ValueType;
        public readonly InputValueKind Kind;

        public ParentBindingDescriptor(ulong id, string path, System.Type valueType, InputValueKind kind)
        {
            Id = id;
            Path = path;
            ValueType = valueType;
            Kind = kind;
        }
    }
    
    public static class ParentBindingRegistry
    {
        private static readonly Dictionary<System.Type, ParentBindingDescriptor[]> BindingsByType = new();

        private static readonly ParentBindingDescriptor[] Empty =
            System.Array.Empty<ParentBindingDescriptor>();

        public static void Register(
            System.Type parentType,
            ParentBindingDescriptor[] descriptors
        )
        {
            if (parentType == null)
                throw new System.ArgumentNullException(nameof(parentType));

            BindingsByType[parentType] = descriptors ?? Empty;
        }

        public static ParentBindingDescriptor[] Get(System.Type parentType)
        {
            if (parentType == null)
                return Empty;

            return BindingsByType.TryGetValue(parentType, out var descriptors)
                ? descriptors
                : Empty;
        }

        public static System.Type[] GetRegisteredTypes()
        {
            if (BindingsByType.Count == 0)
                return System.Array.Empty<System.Type>();

            var types = new System.Type[BindingsByType.Count];
            BindingsByType.Keys.CopyTo(types, 0);

            System.Array.Sort(types, static (a, b) =>
                string.Compare(a.FullName, b.FullName, System.StringComparison.Ordinal));

            return types;
        }

        public static bool IsRegistered(System.Type parentType)
        {
            return parentType != null && BindingsByType.ContainsKey(parentType);
        }
    }
}