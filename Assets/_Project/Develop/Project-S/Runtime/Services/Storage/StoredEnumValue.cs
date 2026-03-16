using System;

namespace Project_S.Runtime.Services.Storage
{
    public class StoredEnumValue<TEnum> : StoredValue<TEnum>
    {
        public StoredEnumValue(string id, DataStorage d, TEnum defaultValue) : base(id, d, defaultValue)
        {
        }

        protected override void Store(string key, TEnum newVal)
        {
            _dataStorage.SetValue(key, newVal.ToString());
        }

        protected override TEnum Read(string key, TEnum defaultValue)
        {
            var value = _dataStorage.GetValue(key, defaultValue.ToString());
            return (TEnum) Enum.Parse(typeof(TEnum), value);
        }
    }
}