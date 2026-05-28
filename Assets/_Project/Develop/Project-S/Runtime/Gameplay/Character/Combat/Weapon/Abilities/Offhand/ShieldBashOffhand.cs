using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Combat;

// Цей скрипт вішається прямо на ПРЕФАБ Важкого Щита (для лівої руки)
public class ShieldBashOffhand : MonoBehaviour, IOffhandAbility
{
    [Header("Налаштування Тарану")]
    public float bashRadius = 1.5f;      // Ширина удару (щит великий, б'є по площі)
    public float bashDistance = 1.5f;    // Дальність ривка
    public float knockbackForce = 15f;   // Сила, з якою вороги відлітають назад

    [Header("Урон")]
    public List<DamageInstance> damageProfile = new List<DamageInstance>();
    public float poiseDamage = 50f;      // Щит повинен сильно ламати баланс

    [Header("Ефекти")]
    public ParticleSystem impactEffect;  // Ефект зіткнення (іскри/пил)

    private Animator _myAnimator;

    private void Awake()
    {
        _myAnimator = GetComponent<Animator>();
    }

    public void ExecuteOffhandAbility(CombatController combatCtrl, Animator rightHandAnim)
    {
        // 1. Анімація удару щитом (якщо є)
        if (_myAnimator != null)
        {
            _myAnimator.SetTrigger("Bash");
        }

        Vector3 startPos = combatCtrl.transform.position + Vector3.up * 1f;
        Vector3 direction = combatCtrl.transform.forward;

        Debug.Log("<color=blue>[Щит]</color> БАШ! Розступайтеся!");

        // 2. Збираємо ВСІХ ворогів перед собою (використовуємо SphereCastAll, бо ворогів може бути декілька)
        RaycastHit[] hits = Physics.SphereCastAll(startPos, bashRadius, direction, bashDistance);
        HashSet<IDamageReceiver> alreadyHit = new HashSet<IDamageReceiver>(); // Щоб не вдарити одного ворога двічі

        foreach (var hit in hits)
        {
            // Ігноруємо самі себе
            if (hit.collider.transform.root == combatCtrl.transform.root) continue;

            // 3. Відштовхуємо ворога фізично (Knockback)
            Rigidbody enemyRb = hit.collider.GetComponentInParent<Rigidbody>();
            if (enemyRb != null && !enemyRb.isKinematic)
            {
                // Штовхаємо ворога у напрямку нашого погляду
                enemyRb.AddForce(direction * knockbackForce, ForceMode.Impulse);
            }

            // 4. Наносимо урон та збиваємо баланс
            IDamageReceiver target = hit.collider.GetComponentInParent<IDamageReceiver>();
            if (target != null && !alreadyHit.Contains(target))
            {
                alreadyHit.Add(target);

                if (impactEffect != null)
                {
                    impactEffect.transform.position = hit.point;
                    impactEffect.Play();
                }

                var request = new DamageRequest(
                    combatCtrl.gameObject,
                    damageProfile,
                    poiseDamage,
                    null
                );

                target.ReceiveDamage(request);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Допоміжний малюнок у редакторі, щоб бачити радіус удару щитом
        Gizmos.color = Color.blue;
        Vector3 startPos = transform.position + Vector3.up * 1f;
        Gizmos.DrawWireSphere(startPos + transform.forward * bashDistance, bashRadius);
    }
}