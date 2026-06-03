using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Stats;

public class HammerWeapon : MonoBehaviour
{
    [Header("Налаштування зарядки (Легка атака)")]
    public float minChargeTime = 0.4f;
    private float currentChargeTime = 0f;

    [Header("Стаміна")]
    public float staminaDrainRate = 15f;

    [Header("Налаштування вибуху (Фізика легкої атаки)")]
    public Transform hitPoint;
    public float explosionRadius = 5f;
    public float explosionForce = 1000f;
    public float upwardModifier = 2f;

    [Header("Налаштування вибуху (Урон легкої атаки)")]
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

            if (anim.GetBool("IsCharging"))
            {
                currentChargeTime += Time.deltaTime;

                bool hasStamina = combatCtrl.DrainStamina(staminaDrainRate * Time.deltaTime);

                if (!hasStamina)
                {
                    anim.SetBool("IsCharging", false);
                    anim.SetTrigger("CancelCharge");
                    combatCtrl.ForceResetToIdle();
                    Debug.Log("<color=red>[Молот]</color> Сили закінчились! Зарядку скасовано.");
                }
            }
            return true;
        }
        // 2. ВІДПУСТИЛИ КНОПКУ
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

    // Цей метод можна викликати через Animation Event для звичайних атак
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