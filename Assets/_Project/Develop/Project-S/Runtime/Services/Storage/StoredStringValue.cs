namespace Project_S.Runtime.Services.Storage
{
    public class StoredStringValue : StoredValue<string>
    {
        public StoredStringValue(string id, DataStorage d, string defaultValue) : base(id, d, defaultValue)
        {
        }

        protected override string Read(string key, string defaultValue)
        {
            return _dataStorage.GetValue(key, defaultValue);
        }

        protected override void Store(string key, string newVal)
        {
            _dataStorage.SetValue(key, newVal);
        }
    }
}