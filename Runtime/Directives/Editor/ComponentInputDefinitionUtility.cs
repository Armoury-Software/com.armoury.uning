using Armoury.UI.Markers;

namespace Armoury.UI.Editor
{
    public static class ComponentInputDefinitionUtility
    {
        public static void Sync(
            ComponentDefinition component,
            InputDescriptor[] descriptors
        )
        {
            /*if (component == null)
                return;

            if (component.Inputs == null)
                return;

            for (var i = 0; i < component.InputCount; i++)
            {
                var binding = component.Inputs[i];

                if (!TryFindDescriptor(
                        descriptors,
                        binding.InputId,
                        out var descriptor
                    ))
                {
                    binding.IsMissing = true;
                    continue;
                }

                binding.IsMissing = false;
                binding.InputIndex = descriptor.Index;
                binding.InputName = descriptor.MemberName;
                binding.InputAlias = descriptor.Alias;
                binding.Kind = descriptor.Kind;
                binding.ValueTypeName = descriptor.ValueType.AssemblyQualifiedName;
            }*/
        }

        private static bool TryFindDescriptor(
            InputDescriptor[] descriptors,
            ulong inputId,
            out InputDescriptor descriptor
        )
        {
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
}