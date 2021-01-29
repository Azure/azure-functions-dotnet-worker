namespace Microsoft.Azure.Functions.Worker
{
    public abstract class OutputBinding<T>
    {
        internal abstract T GetValue();

        public abstract void SetValue(T value);
    }
}
