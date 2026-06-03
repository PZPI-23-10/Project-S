using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Combat;

public class StunStrikeOffhand : MonoBehaviour, IOffhandAbility
{
    [Header("Налаштування Удару")]
    public float hitRadius = 0.6f;
    public float hitDistance = 1.2f;

    [Header("Оглушення (Урон)")]
    // Зазвичай для оглушення ми ставимо смішний урон (напр. 1 HP), 
    // але величезний Poise Damage (напр. 100), щоб гарантовано "зламати" ворога.
    public List<DamageInstance> damageProfile = new List<DamageInstance>();
    public float massivePoiseDamage = 100f;

    [Header("Ефекти")]
    public ParticleSystem stunSparksEffect; // Ефект "зірочок" або іскор
    public AudioClip stunSound;             // Глухий звук удару по шолому/голові

    private Animator _myAnimator;

    private void Awake()
    {
        _myAnimator = GetComponent<Animator>();
    }

    public void ExecuteOffhandAbility(CombatController combatCtrl, Animator rightHandAnim)
    {
        // 1. Анімація самої булави в лівій руці
        if (_myAnimator != null)
        {
            _myAnimator.SetTrigger("Bash");
        }

        Vector3 startPos = combatCtrl.transform.position + Vector3.up * 1.5f; // Б'ємо високо (в голову)
        Vector3 direction = combatCtrl.transform.forward;

        // 2. Шукаємо ворога перед собою
        if (Physics.SphereCast(startPos, hitRadius, direction, out RaycastHit hit, hitDistance))
        {
            IDamageReceiver targetEnemy = hit.collider.GetComponentInParent<IDamageReceiver>();

            if (targetEnemy != null && hit.collider.transform.root != combatCtrl.transform.root)
            {
                Debug.Log($"<color=yellow>[Булава]</color> БОНЬК! Влучили по {hit.collider.name}!");

                if (stunSparksEffect != null)
                {
                    // Переміщуємо ефект іскор у точку влучання і програємо
                    stunSparksEffect.transform.position = hit.point;
                    stunSparksEffect.Play();
                }

                // Якщо є система аудіо, тут можна додати звук:
                // if (stunSound != null) AudioSource.PlayClipAtPoint(stunSound, hit.point);

                // 3. Відправляємо запит на колосальний урон по балансу
                var request = new DamageRequest(
                    combatCtrl.gameObject,
                    damageProfile,
                    massivePoiseDamage,    // <--- Ось тут магія оглушення!
                    null
                );

                targetEnemy.ReceiveDamage(request);
            }
        }
    }
}