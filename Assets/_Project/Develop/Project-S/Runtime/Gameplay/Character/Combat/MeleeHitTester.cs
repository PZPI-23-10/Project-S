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
            if (_collider != null) _collider.enabled = true;
        }

        public void StopHitDetection()
        {
            _isHitboxActive = false;
            _alreadyHit.Clear();
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

            _alreadyHit.Add(other);

            float totalDamage = 0f;
            DamageType primaryType = DamageType.Blunt;

            if (_weaponData.DamageProfile != null && _weaponData.DamageProfile.Count > 0)
            {
                foreach (var damageInstance in _weaponData.DamageProfile)
                    totalDamage += damageInstance.Amount;

                primaryType = _weaponData.DamageProfile[0].Type;
            }

            var buffs = _attacker.GetComponentInParent<BuffController>();
            if (buffs != null)
                totalDamage *= buffs.AttackDamageMultiplier;

            var request = new DamageRequest(
                _attacker,
                totalDamage,
                _weaponData.PoiseDamage,
                primaryType,
                _weaponData);

            receiver.ReceiveDamage(request);
        }
    }
}
