using System;

namespace Project_S.Runtime.Services.Storage
{
    public class StoredDateTimeValue : StoredValue<DateTime>
    {
        public StoredDateTimeValue(string id, DataStorage d, DateTime defaultValue) : base(id, d, defaultValue)
        {
        }

        protected override DateTime Read(string key, DateTime defaultValue)
        {
            var s = _dataStorage.GetValue(key, defaultValue.ToString());
            DateTime res = DateTime.Parse(s);
            return res;
        }

        protected override void Store(string key, DateTime newVal)
        {
            _dataStorage.SetValue(key, newVal.ToString());
        }
    }
}