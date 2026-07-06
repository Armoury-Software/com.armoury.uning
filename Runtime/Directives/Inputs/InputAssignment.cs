namespace Armoury.UI
{
    public readonly struct InputAssignment
    {
        public readonly ulong Id;
        public readonly InputValue Value;

        public InputAssignment(ulong id, in InputValue value)
        {
            Id = id;
            Value = value;
        }
    }
}
