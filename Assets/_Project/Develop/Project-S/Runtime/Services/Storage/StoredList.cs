using System.Collections.Generic;
using Newtonsoft.Json;

namespace Project_S.Runtime.Services.Storage
{
    public class StoredList<TValue> : StoredValue<List<TValue>>
    {
        public StoredList(string id, DataStorage d, List<TValue> defaultValue) : base(id, d, defaultValue) { }

        public void ForceStore()
        {
            Store();
        }

        public void Add(TValue value)
        {
            Value.Add(value);
            OnValueChangedEvent.Invoke();

            Store();
        }

        public bool Remove(TValue value)
        {
            bool result = Value.Remove(value);
            OnValueChangedEvent.Invoke();

            Store();

            return result;
        }

        protected override void Store(string key, List<TValue> value)
        {
            string json = JsonConvert.SerializeObject(value);

            _dataStorage.SetValue(key, json);
        }

        protected override List<TValue> Read(string key, List<TValue> defaultValue)
        {
            string storedString = _dataStorage.GetValue(key, string.Empty);

            if (storedString == string.Empty)
            {
                return new List<TValue>(defaultValue);
            }

            return JsonConvert.DeserializeObject<List<TValue>>(storedString);
        }

        public override void Reset()
        {
            Value = new List<TValue>(DefaultValue);
        }
    }
}