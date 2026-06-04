using System;
using System.Collections;
using Project_S.Runtime.Gameplay.Enemies;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Spawning
{
    public class SpawnedMobHandle : MonoBehaviour
    {
        private MobSpawnZone _zone;
        private MobSpawnDefinition _definition;
        private EnemyHealth _health;
        private bool _slotReleased;

        public event Action<SpawnedMobHandle> SlotReleased;

        public MobSpawnZone Zone => _zone;
        public MobSpawnDefinition Definition => _definition;
        public bool IsAlive => _health == null || !_health.IsDead;

        public void Configure(MobSpawnZone zone, MobSpawnDefinition definition)
        {
            _zone = zone;
            _definition = definition;
            _health = GetComponent<EnemyHealth>();

            if (_health != null)
                _health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.Died -= OnDied;

            ReleaseSlot();
        }

        private void OnDied(EnemyHealth health)
        {
            if (_health != null)
                _health.Died -= OnDied;

            float delay = _definition != null ? _definition.SlotReleaseDelayAfterDeath : 0f;
            StartCoroutine(ReleaseAfterDelay(delay));
        }

        private IEnumerator ReleaseAfterDelay(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            ReleaseSlot();
        }

        private void ReleaseSlot()
        {
            if (_slotReleased)
                return;

            _slotReleased = true;
            SlotReleased?.Invoke(this);
        }
    }
}
