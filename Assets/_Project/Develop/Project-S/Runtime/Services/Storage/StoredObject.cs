using Newtonsoft.Json;

namespace Project_S.Runtime.Services.Storage
{
    public class StoredObject<TValue> : StoredValue<TValue> where TValue : class, new()
    {
        public StoredObject(string id, DataStorage d, TValue defaultValue) : base(id, d, defaultValue)
        {
        }

        public void Save()
        {
            Store();
        }
    
        protected override void Store(string key, TValue newVal)
        {
            string json = JsonConvert.SerializeObject(newVal);
       
            _dataStorage.SetValue(key, json);
        }

        protected override TValue Read(string key, TValue defaultValue)
        {
            string storedString = _dataStorage.GetValue(key, string.Empty);

            if (storedString == string.Empty)
            {
                return new TValue();
            }

            return JsonConvert.DeserializeObject<TValue>(storedString);
        }

        public override void Reset()
        {
            Value = new TValue();
        }
    }
}