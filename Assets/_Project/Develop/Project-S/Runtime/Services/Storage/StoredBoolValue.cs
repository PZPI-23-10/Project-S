namespace Project_S.Runtime.Services.Storage
{
    public class StoredBoolValue : StoredValue<bool>
    {
        public StoredBoolValue(string id, DataStorage d, bool defaultValue) : base(id, d, defaultValue)
        {
        }

        protected override bool Read(string key, bool defaultValue)
        {
            return _dataStorage.GetValue(key, defaultValue);
        }

        protected override void Store(string key, bool newVal)
        {
            _dataStorage.SetValue(key, newVal);
        }
    }
}