using System.Collections; // Обов'язково для корутин (уповільнення часу)
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public class CharacterDamageReceiver : MonoBehaviour, IDamageReceiver
    {
        [Header("Зв'язки")]
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private BlockController _blockController;
        [SerializeField] private CombatController _combatController;
        [SerializeField] private PoiseController _poiseController;
        [SerializeField] private StaminaController _staminaController;

        [Header("Аудіо")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _hurtSound;
        [SerializeField] private AudioClip _blockHitSound;
        [SerializeField] private AudioClip _parrySound;

        // ==========================================
        // ДОДАНО: VFX для парирування
        // ==========================================
        [Header("Ефекти")]
        [SerializeField] private GameObject _parryVFXPrefab; // Префаб іскор або спалаху
        // ==========================================

        public void ReceiveDamage(DamageRequest request)
        {
            DamageRequest modifiedRequest = request;

            // --- ЛОГІКА ЗАХИСТУ ---
            if (_blockController != null && _blockController.IsBlocking)
            {
                modifiedRequest = _blockController.ModifyIncomingDamage(request);

                if (modifiedRequest.HealthDamage == 0f && request.HealthDamage > 0f)
                {
                    Debug.Log("<color=cyan>[ЗАХИСТ]</color> ПАРИРУВАННЯ! Енергію збережено.");

                    if (_audioSource != null && _parrySound != null)
                        _audioSource.PlayOneShot(_parrySound);

                    if (_combatController != null && _combatController.CurrentWeapon != null)
                    {
                        _stats.Add(StatType.Stamina, _combatController.CurrentWeapon.ParryStaminaReward);
                    }

                    // ==========================================
                    // ВИКЛИКАЄМО ЕФЕКТИ ПАРИРУВАННЯ
                    // ==========================================
                    // 1. Спавнимо іскри (якщо ти додав префаб)
                    if (_parryVFXPrefab != null)
                    {
                        Vector3 hitPoint = request.Source != null ? request.Source.transform.position : transform.position + transform.forward;
                        Vector3 spawnPos = Vector3.Lerp(transform.position, hitPoint, 0.5f) + Vector3.up * 1.5f; // Посередині між вами

                        Destroy(Instantiate(_parryVFXPrefab, spawnPos, Quaternion.identity), 2f);
                    }

                    // 2. УПОВІЛЬНЕННЯ ЧАСУ (Epic Hit Stop)
                    StartCoroutine(HitStopRoutine());
                    // ==========================================
                }
                else
                {
                    float staminaCost = request.HealthDamage * 0.5f;

                    if (_staminaController != null)
                    {
                        if (_staminaController.Spend(staminaCost))
                        {
                            Debug.Log($"<color=blue>[ЗАХИСТ]</color> БЛОК! Витрачено стаміни: {staminaCost}");
                            if (_audioSource != null && _blockHitSound != null)
                                _audioSource.PlayOneShot(_blockHitSound);
                        }
                        else
                        {
                            modifiedRequest = request;
                            Debug.LogWarning("<color=red>[ЗАХИСТ]</color> ПРОБИТТЯ БЛОКУ! Не вистачило енергії.");
                        }
                    }
                }
            }

            // --- ЗАСТОСУВАННЯ ФІНАЛЬНОГО УРОНУ ---
            if (_stats != null)
            {
                if (modifiedRequest.HealthDamage > 0)
                {
                    _stats.Add(StatType.Health, -modifiedRequest.HealthDamage);

                    if (_audioSource != null && _hurtSound != null)
                    {
                        _audioSource.pitch = Random.Range(0.9f, 1.1f);
                        _audioSource.PlayOneShot(_hurtSound);
                    }
                }

                if (modifiedRequest.PoiseDamage > 0)
                {
                    if (_poiseController != null)
                    {
                        Vector3 attackerPos = request.Source != null ? request.Source.transform.position : transform.position + transform.forward;
                        _poiseController.ApplyPoiseDamage(modifiedRequest.PoiseDamage, attackerPos);
                    }
                    else
                    {
                        _stats.Add(StatType.Poise, -modifiedRequest.PoiseDamage);
                    }
                }
            }
        }

        // ==========================================
        // КОРУТИНА ДЛЯ УПОВІЛЬНЕННЯ ЧАСУ (HIT STOP)
        // ==========================================
        private IEnumerator HitStopRoutine()
        {
            Time.timeScale = 0.1f; // Уповільнюємо час до 10%
            yield return new WaitForSecondsRealtime(0.15f); // Чекаємо реальні 0.15 секунд
            Time.timeScale = 1f; // Повертаємо час у норму
        }
    }
}