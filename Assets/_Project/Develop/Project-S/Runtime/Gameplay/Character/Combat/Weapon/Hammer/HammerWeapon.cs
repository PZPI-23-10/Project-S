using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Stats;

public class HammerWeapon : MonoBehaviour
{
    [Header("Налаштування зарядки")]
    public float minChargeTime = 0.4f;
    private float currentChargeTime = 0f;

    [Header("Стаміна")]
    public float staminaDrainRate = 15f; // Скільки стаміни витрачається за ОДНУ секунду утримання!

    [Header("Налаштування вибуху (Фізика)")]
    public Transform hitPoint;
    public float explosionRadius = 5f;
    public float explosionForce = 1000f;
    public float upwardModifier = 2f;

    [Header("Налаштування вибуху (Урон)")]
    public bool useIndependentDamage = true;
    public List<DamageInstance> explosionDamageProfile = new List<DamageInstance>();
    public float explosionPoiseDamage = 15f;

    [Header("Ефекти")]
    public ParticleSystem dustEffect;

    private CombatController _cachedCombatCtrl;

    public bool ProcessCustomInput(PlayerInputSnapshot input, Animator anim, CombatController combatCtrl)
    {
        _cachedCombatCtrl = combatCtrl;

        // 1. ПОКИ ТРИМАЄМО КНОПКУ АТАКИ
        if (input.LightAttackHeld)
        {
            if (!anim.GetBool("IsCharging"))
            {
                anim.SetBool("IsCharging", true);
                currentChargeTime = 0f;
            }

            // Якщо ми у стані зарядки (молот нагорі)
            if (anim.GetBool("IsCharging"))
            {
                currentChargeTime += Time.deltaTime;

                // Витрачаємо стаміну плавно (множимо на Time.deltaTime)
                bool hasStamina = combatCtrl.DrainStamina(staminaDrainRate * Time.deltaTime);

                // Якщо гравець перетримав і сили закінчились повністю!
                if (!hasStamina)
                {
                    anim.SetBool("IsCharging", false);
                    anim.SetTrigger("CancelCharge"); // Спускаємо молот без удару
                    combatCtrl.ForceResetToIdle();
                    Debug.Log("<color=red>[Молот]</color> Сили закінчились! Зарядку скасовано.");
                }
            }
            return true;
        }
        // 2. ВІДПУСТИЛИ КНОПКУ (і сили ще були)
        else if (anim.GetBool("IsCharging"))
        {
            anim.SetBool("IsCharging", false);

            if (currentChargeTime >= minChargeTime)
            {
                combatCtrl.PerformLightAttack();
            }
            else
            {
                anim.SetTrigger("CancelCharge");
                combatCtrl.ForceResetToIdle();
            }
            currentChargeTime = 0f;
            return true;
        }

        return false;
    }

    public void SmashGround()
    {
        if (dustEffect != null) dustEffect.Play();

        Collider[] hitColliders = Physics.OverlapSphere(hitPoint.position, explosionRadius);
        HashSet<IDamageReceiver> alreadyHit = new HashSet<IDamageReceiver>();

        foreach (Collider hit in hitColliders)
        {
            if (hit.transform.root == this.transform.root) continue;

            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, hitPoint.position, explosionRadius, upwardModifier, ForceMode.Impulse);
            }

            if (_cachedCombatCtrl != null)
            {
                IDamageReceiver receiver = hit.GetComponentInParent<IDamageReceiver>();

                if (receiver != null && !alreadyHit.Contains(receiver))
                {
                    alreadyHit.Add(receiver);

                    WeaponItemData weaponData = _cachedCombatCtrl.ActiveWeapon;
                    GameObject attacker = _cachedCombatCtrl.gameObject;

                    var damageProfile = useIndependentDamage ? explosionDamageProfile : weaponData.DamageProfile;
                    float poiseDamage = useIndependentDamage ? explosionPoiseDamage : weaponData.PoiseDamage;

                    var buffs = attacker.GetComponentInParent<BuffController>();
                    if (buffs != null)
                    {
                        damageProfile = buffs.ModifyDamageProfile(damageProfile);
                    }

                    var request = new DamageRequest(
                        attacker,
                        damageProfile,
                        poiseDamage,
                        weaponData);

                    receiver.ReceiveDamage(request);
                    _cachedCombatCtrl.AddChargeOnHit();
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (hitPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(hitPoint.position, explosionRadius);
        }
    }
}