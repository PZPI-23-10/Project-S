using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Stats;
using KinematicCharacterController; // Для роботи зі стрибком KCC

[CreateAssetMenu(fileName = "Earthquake", menuName = "Project-S/Abilities/Actives/Earthquake")]
public class EarthquakeAbility : WeaponActiveData
{
    [Header("Налаштування стрибка (KCC)")]
    public float jumpUpForce = 15f;
    public float jumpForwardForce = 10f;

    [Header("Налаштування вибуху")]
    public float radius = 4f;
    [Tooltip("На якій висоті від землі вибух ще наносить урон (щоб не бити літаючих)")]
    public float maxHitHeight = 1.5f;
    public float poiseDamage = 200f;
    public List<DamageInstance> explosionDamage = new List<DamageInstance>();

    public override bool ResetChargeOnUse => true;

    // --- СТРИБОК ЧЕРЕЗ KCC ---
    public void StartJump(CombatController combatCtrl)
    {
        KinematicCharacterMotor motor = combatCtrl.GetComponentInParent<KinematicCharacterMotor>();
        if (motor != null)
        {
            motor.ForceUnground(); // Відриваємо гравця від землі
            motor.BaseVelocity = (combatCtrl.transform.up * jumpUpForce) + (combatCtrl.transform.forward * jumpForwardForce);
        }
    }

    // --- ВИБУХ ---
    public override void ExecuteHeavyAbility(CombatController combatCtrl, Animator anim, GameObject weaponModel)
    {
        Transform hitPoint = FindDeepChild(weaponModel.transform, "HitPoint");
        if (hitPoint == null) hitPoint = weaponModel.transform;

        // Малюємо лінію для тесту у вікні Scene
        Debug.DrawRay(hitPoint.position, Vector3.up * 5f, Color.red, 2f);
        Debug.Log($"<color=orange>ВИБУХ ЗЕМЛЕТРУСУ!</color> Радіус: {radius}");

        ParticleSystem groundEffect = hitPoint.GetComponentInChildren<ParticleSystem>();
        if (groundEffect != null) groundEffect.Play();

        Collider[] hitColliders = Physics.OverlapSphere(hitPoint.position, radius);
        HashSet<IDamageReceiver> alreadyHit = new HashSet<IDamageReceiver>();

        foreach (Collider col in hitColliders)
        {
            if (col.transform.root == combatCtrl.transform.root) continue;

            // Перевірка на літаючих ворогів
            float enemyBottomY = col.bounds.min.y;
            float heightDifference = enemyBottomY - hitPoint.position.y;

            if (heightDifference > maxHitHeight)
            {
                Debug.Log($"<color=grey>Проігноровано літаючого ворога: {col.name}</color>");
                continue;
            }

            IDamageReceiver receiver = col.GetComponentInParent<IDamageReceiver>();
            if (receiver != null && !alreadyHit.Contains(receiver))
            {
                alreadyHit.Add(receiver);
                ApplyDamage(combatCtrl, receiver);
            }
        }
    }

    private void ApplyDamage(CombatController combatCtrl, IDamageReceiver receiver)
    {
        var request = new DamageRequest(combatCtrl.gameObject, new List<DamageInstance>(explosionDamage), poiseDamage, combatCtrl.ActiveWeapon);
        receiver.ReceiveDamage(request);
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}