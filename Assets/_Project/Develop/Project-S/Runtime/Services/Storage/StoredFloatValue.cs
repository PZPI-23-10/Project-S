namespace Project_S.Runtime.Services.Storage
{
    public class StoredFloatValue : StoredValue<float>
    {
        public StoredFloatValue(string id, DataStorage d, float defaultValue) : base(id, d, defaultValue)
        {
        }

        protected override float Read(string key, float defaultValue)
        {
            return _dataStorage.GetValue(key, defaultValue);
        }

        protected override void Store(string key, float newVal)
        {
            _dataStorage.SetValue(key, newVal);
        }
    }
}