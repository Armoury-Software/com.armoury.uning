namespace Armoury.UI
{
    public static class InputApplier
    {
        public static void Apply(object target, InputAssignment[] inputs, int inputCount)
        {
            if (target == null)
                return;

            if (inputs == null || inputCount <= 0)
                return;

            if (target is not IInputReceiver receiver)
                return;

            var changed = default(InputChangeMask);

            for (var i = 0; i < inputCount; i++)
            {
                ref readonly var input = ref inputs[i];

                receiver.SetInput(input.Id, in input.Value, ref changed);
            }

            if (!changed.IsEmpty && target is Directive directive)
            {
                directive.__UniNgOnInputsChanged(changed);
            }
        }
    }
}