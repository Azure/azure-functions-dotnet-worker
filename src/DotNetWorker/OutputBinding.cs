namespace Microsoft.Azure.Functions.Worker
{
    public class OutputBinding<T>
    {
        private T val;

        public T GetValue()
        {
            return val;
        }

        public void SetValue(T value)
        {
            val = value;
        }
    }
}
