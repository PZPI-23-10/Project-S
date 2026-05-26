using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    [RequireComponent(typeof(Collider))]
    public class MeleeHitTester : MonoBehaviour
    {
        private WeaponItemData _weaponData;
        private GameObject _attacker;
        private bool _isHitboxActive;
        private Collider _collider;

        private readonly HashSet<Collider> _alreadyHit = new HashSet<Collider>();
        private readonly HashSet<IDamageReceiver> _alreadyDamagedEnemies = new HashSet<IDamageReceiver>();

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_collider != null) _collider.enabled = false;
        }

        public void Setup(WeaponItemData data, GameObject attacker)
        {
            _weaponData = data;
            _attacker = attacker;

            if (_collider != null) _collider.isTrigger = true;
        }

        public void StartHitDetection()
        {
            _isHitboxActive = true;
            _alreadyHit.Clear();
            _alreadyDamagedEnemies.Clear();
            if (_collider != null) _collider.enabled = true;
        }

        public void StopHitDetection()
        {
            _isHitboxActive = false;
            _alreadyHit.Clear();
            _alreadyDamagedEnemies.Clear();
            if (_collider != null) _collider.enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isHitboxActive || _weaponData == null || _attacker == null)
                return;

            if (other.transform.root == _attacker.transform.root)
                return;

            if (_alreadyHit.Contains(other))
                return;

            IDamageReceiver receiver = other.GetComponentInParent<IDamageReceiver>();
            if (receiver == null)
                return;

            if (_alreadyDamagedEnemies.Contains(receiver))
                return;

            _alreadyDamagedEnemies.Add(receiver); 

            _alreadyHit.Add(other);

            var currentDamageProfile = new List<DamageInstance>(_weaponData.DamageProfile);
            float currentPoiseDamage = _weaponData.PoiseDamage;

            var buffs = _attacker.GetComponentInParent<BuffController>();
            if (buffs != null)
                currentDamageProfile = buffs.ModifyDamageProfile(currentDamageProfile);

            var combatController = _attacker.GetComponent<CombatController>();

            // --- ЗМІНЕНО: Читаємо пасивки прямо з WeaponItemData (ScriptableObject) ---
            if (_weaponData.Passives != null)
            {
                foreach (var passive in _weaponData.Passives)
                {
                    if (passive != null)
                    {
                        passive.OnBeforeHit(combatController, other, ref currentPoiseDamage, ref currentDamageProfile);
                    }
                }
            }
            // --------------------------------------------------------------------------

            var request = new DamageRequest(
                            _attacker,
                            currentDamageProfile,
                            currentPoiseDamage,
                            _weaponData);

            receiver.ReceiveDamage(request);

            // --- ЗМІНЕНО: Читаємо пасивки після удару з WeaponItemData ---
            if (_weaponData.Passives != null)
            {
                foreach (var passive in _weaponData.Passives)
                {
                    if (passive != null)
                    {
                        passive.OnAfterHit(combatController, other, receiver);
                    }
                }
            }
            // -------------------------------------------------------------

            if (combatController != null)
            {
                combatController.AddChargeOnHit();
            }
        }
    }
}