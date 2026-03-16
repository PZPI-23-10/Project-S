using UnityEngine.Events;

namespace Project_S.Runtime.Services.Storage
{
    public static class StoredValueExtensions
    {
        public static void SafeDispose<T>(this StoredValue<T> resetable)
        {
            if (resetable != null)
            {
                resetable.Dispose();
            }
        }
    }

    public abstract class StoredValue<T> : IResetable
    {
        protected DataStorage _dataStorage;
        string _id;
    
        protected T DefaultValue;

        public StoredValue(string id, DataStorage d, T defaultValue)
        {
            _id = id;
            _dataStorage = d;
            DefaultValue = defaultValue;
            _valueHashed = Read(_id, defaultValue);
            _dataStorage.AddResetable(this);
        }

        internal UnityEvent OnValueChangedEvent = new UnityEvent();

        T _valueHashed;

        public T Value
        {
            get => _valueHashed;
            internal set
            {
                if (!System.Object.Equals(value, _valueHashed))
                {
                    _valueHashed = value;
                    Store(_id, _valueHashed);

                    OnValueChangedEvent.Invoke();
                }
            }
        }

        protected void Store()
        {
            Store(_id, _valueHashed);
        }

        public virtual void Reset()
        {
            this.Value = DefaultValue;
        }

        public void Dispose()
        {
            _dataStorage.RemoveResetable(this);
        }
    
        public virtual void Release()
        {
            _dataStorage.ReleaseStored(_id, this);
        }

        protected abstract void Store(string key, T newVal);

        protected abstract T Read(string key, T defaultValue);
    }
}