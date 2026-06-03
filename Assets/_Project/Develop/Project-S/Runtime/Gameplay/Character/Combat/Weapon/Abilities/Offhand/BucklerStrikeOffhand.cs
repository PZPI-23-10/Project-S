using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Combat;

// Вішається на префаб БАКЛЕРА (маленького щита) для лівої руки
public class BucklerStrikeOffhand : MonoBehaviour, IOffhandAbility
{
    [Header("Налаштування Стусана")]
    public float punchRadius = 0.4f;      // Баклер маленький, тому радіус невеликий
    public float punchDistance = 1.0f;    // Б'ємо майже впритул

    [Header("Урон (Баклер)")]
    public List<DamageInstance> damageProfile = new List<DamageInstance>();
    public float poiseDamage = 35f;       // Добре збиває атаки дрібних мобів

    [Header("Ефекти")]
    public ParticleSystem punchEffect;

    private Animator _myAnimator;

    private void Awake()
    {
        _myAnimator = GetComponent<Animator>();
    }

    public void ExecuteOffhandAbility(CombatController combatCtrl, Animator rightHandAnim)
    {
        if (_myAnimator != null)
        {
            _myAnimator.SetTrigger("Punch");
        }

        Debug.Log("<color=yellow>[Баклер]</color> Швидкий стусан!");

        Vector3 startPos = combatCtrl.transform.position + Vector3.up * 1.2f;
        Vector3 direction = combatCtrl.transform.forward;

        // Для баклера достатньо звичайного SphereCast (б'ємо тільки одну ціль прямо перед собою)
        if (Physics.SphereCast(startPos, punchRadius, direction, out RaycastHit hit, punchDistance))
        {
            IDamageReceiver target = hit.collider.GetComponentInParent<IDamageReceiver>();

            if (target != null && hit.collider.transform.root != combatCtrl.transform.root)
            {
                if (punchEffect != null)
                {
                    punchEffect.transform.position = hit.point;
                    punchEffect.Play();
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
}