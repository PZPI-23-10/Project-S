namespace Project_S.Runtime.Services.Storage
{
    public class StoredIntValue : StoredValue<int>
    {
        public StoredIntValue(string id, DataStorage d, int defaultValue) : base(id, d, defaultValue)
        {
        }

        protected override int Read(string key, int defaultValue)
        {
            int res = _dataStorage.GetValue(key, defaultValue);
            return res;
        }

        protected override void Store(string key, int newVal)
        {
            _dataStorage.SetValue(key, newVal);
        }
    }
}