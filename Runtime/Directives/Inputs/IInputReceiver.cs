namespace Armoury.UI
{
    public interface IInputReceiver
    {
        bool SetInput(ulong inputId, in InputValue value, ref InputChangeMask changed);
    }
}
