using System.Collections.Generic;
using UnityEngine;

namespace Project_S.Runtime.Services.Storage
{
    public class DataStorage
    {
        private readonly List<IResetable> _resetables = new List<IResetable>();
    
        public void AddResetable(IResetable resetable)
        {
            _resetables.Add(resetable);
        }
    
        public void RemoveResetable(IResetable resetable)
        {
            _resetables.Remove(resetable);
        }

        public void Reset()
        {
            for (int i = 0; i < _resetables.Count; i++)
            {
                _resetables[i].Reset();
            }
        
            PlayerPrefs.DeleteAll();
        }

        public string GetValue(string key, string val)
        {
            return PlayerPrefs.GetString(key, val);
        }

        public void SetValue(string key, string val)
        {
            PlayerPrefs.SetString(key, val);
        }

        public int GetValue(string key, int val)
        {
            return PlayerPrefs.GetInt(key, val);
        }

        internal float GetValue(string v1, float v2)
        {
            return PlayerPrefs.GetFloat(v1, v2);
        }

        internal long GetValue(string v1, long v2)
        {
            return (long)PlayerPrefs.GetFloat(v1, v2);
        }

        public void SetValue(string key, int val)
        {
            PlayerPrefs.SetInt(key, val);
        }

        public bool GetValue(string key, bool val)
        {
            int def;
            if (val)
            {
                def = 1;
            }
            else
            {
                def = 0;
            }
            return PlayerPrefs.GetInt(key, def) == 1;
        }

        public void SetValue(string key, bool state)
        {
            PlayerPrefs.SetInt(key, state ? 1 : 0);
        }

        internal void SetValue(string key, float val)
        {
            PlayerPrefs.SetFloat(key, val);
        }

        public void SaveData()
        {
            PlayerPrefs.Save();
        }

        public void ReleaseStored(string key, IResetable resetable)
        {
            PlayerPrefs.DeleteKey(key);
            _resetables.Remove(resetable);
        }
    }
}