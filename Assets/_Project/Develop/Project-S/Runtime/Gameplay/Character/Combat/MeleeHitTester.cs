using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;
using Project_S.Runtime.Gameplay.Harvesting;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    [RequireComponent(typeof(Collider))]
    public class MeleeHitTester : MonoBehaviour
    {
        private WeaponItemData _weaponData;
        private GameObject _attacker;
        private bool _isHitboxActive;
        private Collider _collider;
        private Rigidbody _rigidbody;

        private readonly HashSet<Collider> _alreadyHit = new HashSet<Collider>();
        private readonly HashSet<IDamageReceiver> _alreadyDamagedEnemies = new HashSet<IDamageReceiver>();

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_collider != null) _collider.enabled = false;

            EnsureRigidbody();
        }

        public void Setup(WeaponItemData data, GameObject attacker)
        {
            _weaponData = data;
            _attacker = attacker;

            if (_collider != null) _collider.isTrigger = true;
            EnsureRigidbody();
        }

        public void StartHitDetection()
        {
            _isHitboxActive = true;
            _alreadyHit.Clear();
            _alreadyDamagedEnemies.Clear();
            if (_collider != null) _collider.enabled = true;
            Physics.SyncTransforms();
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
            TryDamage(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryDamage(other);
        }

        private void TryDamage(Collider other)
        {
            if (!_isHitboxActive || _weaponData == null || _attacker == null)
                return;

            if (other == null)
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

            if (_weaponData.Passives != null)
            {
                foreach (var passive in _weaponData.Passives)
                {
                    if (passive != null)
                        passive.OnBeforeHit(combatController, other, ref currentPoiseDamage, ref currentDamageProfile);
                }
            }

            var request = new DamageRequest(
                            _attacker,
                            currentDamageProfile,
                            currentPoiseDamage,
                            _weaponData);

            receiver.ReceiveDamage(request);

            // ==============================================================
            // СПАВН КРОВІ / ІСКОР ТА ЗВУК ПОВЕРХНІ
            // ==============================================================
            HitSurface surface = other.GetComponentInParent<HitSurface>();
            if (surface != null)
            {
                // Спавн візуалу (кров/деревина/іскри)
                if (surface.HitVFXPrefab != null)
                {
                    Vector3 hitPoint = other.ClosestPoint(transform.position);
                    GameObject vfx = Instantiate(surface.HitVFXPrefab, hitPoint, Quaternion.LookRotation(_attacker.transform.forward));
                    Destroy(vfx, 2f);
                }

                // Звук удару по конкретній поверхні
                if (surface.SurfaceHitSound != null && combatController != null)
                {
                    combatController.PlayHitSound(surface.SurfaceHitSound);
                }
            }
            // Якщо на об'єкті немає скрипта HitSurface, але це живий ворог - граємо дефолтний звук зброї
            else if (!(receiver is HarvestableResourceNode))
            {
                if (_weaponData.HitSound != null && combatController != null)
                {
                    combatController.PlayHitSound(_weaponData.HitSound);
                }
            }

            // ==============================================================
            // ДОДАТКОВИЙ ЗВУК ВІД ЗМАЗКИ (Електричний удар)
            // ==============================================================
            if (combatController != null && combatController.ActiveCoatingHitSound != null)
            {
                combatController.PlayHitSound(combatController.ActiveCoatingHitSound);
            }
            // ==============================================================


            // --- Читаємо пасивки після удару ---
            if (_weaponData.Passives != null)
            {
                foreach (var passive in _weaponData.Passives)
                {
                    if (passive != null)
                        passive.OnAfterHit(combatController, other, receiver);
                }
            }

            if (combatController != null)
            {
                if (_attacker.GetComponent<MaceRageBuff>() == null)
                {
                    combatController.AddChargeOnHit();
                }

                // ЗУПИНКА ЧАСУ
                combatController.TriggerHitImpact();
            }
        }

        private void EnsureRigidbody()
        {
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();

            if (_rigidbody == null)
                _rigidbody = gameObject.AddComponent<Rigidbody>();

            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
    }
}