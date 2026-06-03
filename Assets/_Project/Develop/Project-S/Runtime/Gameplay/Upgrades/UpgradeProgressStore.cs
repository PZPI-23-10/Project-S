using System;
using System.Collections.Generic;
using Project_S.Runtime.Services.Storage;

namespace Project_S.Runtime.Gameplay.Upgrades
{
    public class UpgradeProgressStore : IDisposable
    {
        private const string DefaultKey = "Upgrades.PurchasedIds";

        private readonly StoredList<string> _storedIds;
        private readonly HashSet<string> _runtimeIds = new HashSet<string>();

        public UpgradeProgressStore(PlayerStorage playerStorage, string key = DefaultKey)
            : this(playerStorage != null ? playerStorage.DataStorage : null, key)
        {
        }

        public UpgradeProgressStore(DataStorage dataStorage, string key = DefaultKey)
        {
            _storedIds = dataStorage != null
                ? new StoredList<string>(key, dataStorage, new List<string>())
                : null;

            foreach (var id in ReadIds())
            {
                if (!string.IsNullOrWhiteSpace(id))
                    _runtimeIds.Add(id);
            }
        }

        public IReadOnlyCollection<string> PurchasedIds => _runtimeIds;

        public bool Has(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && _runtimeIds.Contains(id);
        }

        public bool Add(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !_runtimeIds.Add(id))
                return false;

            if (_storedIds != null)
            {
                if (_storedIds.Value == null)
                    _storedIds.Value = new List<string>();

                if (!_storedIds.Value.Contains(id))
                    _storedIds.Add(id);
            }

            return true;
        }

        public void Dispose()
        {
            _storedIds.SafeDispose();
        }

        private IEnumerable<string> ReadIds()
        {
            return _storedIds != null ? _storedIds.Value ?? new List<string>() : _runtimeIds;
        }
    }
}
