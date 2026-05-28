using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Combat;

public class ThrowingKnifeOffhand : MonoBehaviour, IOffhandAbility
{
    [Header("Налаштування кидка")]
    public GameObject knifeProjectilePrefab; // Сюди закинеш префаб ножа, що летить
    public float throwForce = 25f;           // Швидкість польоту

    [Header("Урон")]
    public List<DamageInstance> damageProfile = new List<DamageInstance>();
    public float poiseDamage = 10f;

    private Animator _myAnimator;

    private void Awake()
    {
        _myAnimator = GetComponent<Animator>();
    }

    public void ExecuteOffhandAbility(CombatController combatCtrl, Animator rightHandAnim)
    {
        // 1. Анімація кидка (якщо на лівій руці є аніматор)
        if (_myAnimator != null)
        {
            _myAnimator.SetTrigger("Throw");
        }

        Debug.Log("<color=cyan>[Метальний ніж]</color> Кидок!");

        if (knifeProjectilePrefab != null)
        {
            // Вираховуємо точку появи ножа (трохи перед гравцем і на рівні грудей)
            Vector3 spawnPos = combatCtrl.transform.position + Vector3.up * 1.3f + combatCtrl.transform.forward * 0.5f;

            // 2. Спавнимо ніж
            GameObject projectile = Instantiate(knifeProjectilePrefab, spawnPos, combatCtrl.transform.rotation);

            // 3. Знаходимо на ножі скрипт польоту і передаємо йому дані
            if (projectile.TryGetComponent(out ThrowingKnifeProjectile knifeScript))
            {
                knifeScript.Setup(combatCtrl.gameObject, damageProfile, poiseDamage, throwForce);
            }
        }
        else
        {
            Debug.LogWarning("Не призначено префаб снаряда (knifeProjectilePrefab)!");
        }
    }
}