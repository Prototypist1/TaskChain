namespace Prototypist.TaskChain
{
    public class Concurrent<TValue> 
    {
        private volatile object value;

        public TValue GetValue() {
            return (TValue)value;
        }

        public void SetValue(TValue value) {
            this.value = value;
        }
    }
}
