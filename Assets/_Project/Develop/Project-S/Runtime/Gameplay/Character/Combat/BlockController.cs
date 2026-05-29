using System.Collections.Generic;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public class BlockController : MonoBehaviour
    {
        [SerializeField] private CombatController _combatController;

        [Header("¿Û‰≥Ó")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _blockStartSound; // «‚ÛÍ Ô≥‰ÌˇÚÚˇ Á·Óø ‰Îˇ ·ÎÓÍÛ

        private float _blockStartedAt;

        public bool IsBlocking { get; private set; }

        public void StartBlock()
        {
            IsBlocking = true;
            _blockStartedAt = Time.time;

            if (GetComponentInChildren<Renderer>() != null)
                GetComponentInChildren<Renderer>().material.color = Color.blue;

            // √–¿™ÃŒ «¬”  ¡ÀŒ ”
            if (_audioSource != null && _blockStartSound != null)
            {
                _audioSource.pitch = Random.Range(0.9f, 1.1f);
                _audioSource.PlayOneShot(_blockStartSound);
            }
        }

        public void StopBlock()
        {
            IsBlocking = false;

            if (GetComponentInChildren<Renderer>() != null)
                GetComponentInChildren<Renderer>().material.color = Color.white;
        }

        public bool IsParryWindow()
        {
            if (!IsBlocking || _combatController == null || _combatController.CurrentWeapon == null)
                return false;

            return Time.time - _blockStartedAt <= _combatController.CurrentWeapon.ParryWindow;
        }

        public DamageRequest ModifyIncomingDamage(DamageRequest request)
        {
            if (!IsBlocking || _combatController == null || _combatController.CurrentWeapon == null)
                return request;

            var weapon = _combatController.CurrentWeapon;

            if (IsParryWindow())
            {
                Debug.Log("<color=green>[¡ÎÓÍ]</color> ≤ƒ≈¿À‹Õ≈ œ¿–»–”¬¿ÕÕﬂ!");
                return new DamageRequest(request.Source, 0f, 0f, request.Type, request.Weapon);
            }

            float damageMultiplier = 1f - weapon.BlockMitigation;

            var reducedProfile = new List<DamageInstance>();
            if (request.DamageProfile != null)
            {
                foreach (var damage in request.DamageProfile)
                {
                    reducedProfile.Add(new DamageInstance
                    {
                        Type = damage.Type,
                        Amount = damage.Amount * damageMultiplier
                    });
                }
            }

            return new DamageRequest(
                request.Source,
                reducedProfile,
                request.PoiseDamage * damageMultiplier,
                request.Weapon);
        }
    }
}