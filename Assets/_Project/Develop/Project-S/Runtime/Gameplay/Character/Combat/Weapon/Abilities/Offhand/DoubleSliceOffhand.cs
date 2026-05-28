using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Combat;

// Вішається на префаб МЕЧА для лівої руки
public class DoubleSliceOffhand : MonoBehaviour, IOffhandAbility
{
    [Header("Налаштування Змаху")]
    public float sliceRadius = 1.2f;     // Ширина змаху
    public float sliceDistance = 1.5f;   // Дальність удару мечем

    [Header("Урон")]
    public List<DamageInstance> damageProfile = new List<DamageInstance>();
    public float poiseDamage = 15f;      // Меч не сильно ламає баланс, він скоріше ріже

    [Header("Ефекти")]
    public ParticleSystem slashTrailEffect; // Ефект дуги від меча

    private Animator _myAnimator;

    private void Awake()
    {
        _myAnimator = GetComponent<Animator>();
    }

    public void ExecuteOffhandAbility(CombatController combatCtrl, Animator rightHandAnim)
    {
        if (_myAnimator != null)
        {
            _myAnimator.SetTrigger("Slice"); // Анімація змаху лівою рукою
        }

        Debug.Log("<color=cyan>[Лівий Меч]</color> Додатковий розріз!");

        if (slashTrailEffect != null) slashTrailEffect.Play();

        Vector3 startPos = combatCtrl.transform.position + Vector3.up * 1f;
        Vector3 direction = combatCtrl.transform.forward;

        // Використовуємо SphereCastAll, щоб зачепити всіх ворогів, які стоять перед нами
        RaycastHit[] hits = Physics.SphereCastAll(startPos, sliceRadius, direction, sliceDistance);
        HashSet<IDamageReceiver> alreadyHit = new HashSet<IDamageReceiver>();

        foreach (var hit in hits)
        {
            if (hit.collider.transform.root == combatCtrl.transform.root) continue;

            IDamageReceiver target = hit.collider.GetComponentInParent<IDamageReceiver>();
            if (target != null && !alreadyHit.Contains(target))
            {
                alreadyHit.Add(target);

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