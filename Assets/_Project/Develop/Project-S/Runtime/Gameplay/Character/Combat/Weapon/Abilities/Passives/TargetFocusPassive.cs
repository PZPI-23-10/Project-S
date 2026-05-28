using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Stats;

[CreateAssetMenu(fileName = "TargetFocus", menuName = "Project-S/Abilities/Passives/TargetFocus")]
public class TargetFocusPassive : WeaponPassiveData
{
    [Header("Налаштування бонусів")]
    public float enemyDamageBonus = 0.15f;    // +15% за стак по ворогу
    public float resourceDamageBonus = 0.30f; // +30% за стак по ресурсу

    // --- НОВЕ: Максимальний ліміт стаків ---
    [Tooltip("Максимальна кількість стаків, яку можна накопичити")]
    public int maxStacks = 5;

    private class Tracker
    {
        public Transform lastTargetRoot;
        public int stacks;
    }

    private Dictionary<CombatController, Tracker> _trackers = new Dictionary<CombatController, Tracker>();

    private Tracker GetTracker(CombatController attacker)
    {
        if (!_trackers.ContainsKey(attacker))
            _trackers[attacker] = new Tracker();
        return _trackers[attacker];
    }

    public override void OnBeforeHit(CombatController attacker, Collider target, ref float poiseDamage, ref List<DamageInstance> damageProfile)
    {
        if (attacker == null) return;

        Tracker tracker = GetTracker(attacker);
        Transform currentTargetRoot = target.transform.root;

        if (tracker.lastTargetRoot != currentTargetRoot)
        {
            tracker.stacks = 0;
            tracker.lastTargetRoot = currentTargetRoot;
            Debug.Log("<color=orange>[Ціль]</color> Нова ціль! Стаки скинуто.");
        }

        if (tracker.stacks > 0)
        {
            // УВАГА: Заміни "Resource" на свій тег ресурсу
            bool isResource = target.CompareTag("Resource");

            float bonusPerStack = isResource ? resourceDamageBonus : enemyDamageBonus;
            float multiplier = 1f + (tracker.stacks * bonusPerStack);

            for (int i = 0; i < damageProfile.Count; i++)
            {
                var damage = damageProfile[i];
                damage.Amount *= multiplier;
                damageProfile[i] = damage;
            }

            Debug.Log($"<color=orange>[Ціль]</color> Урон збільшено на {Mathf.RoundToInt(tracker.stacks * bonusPerStack * 100)}%! (Стаків: {tracker.stacks}/{maxStacks})");
        }
    }

    public override void OnAfterHit(CombatController attacker, Collider target, IDamageReceiver receiver)
    {
        if (attacker == null) return;

        Tracker tracker = GetTracker(attacker);
        Transform currentTargetRoot = target.transform.root;

        if (tracker.lastTargetRoot == currentTargetRoot)
        {
            // --- НОВЕ: Перевіряємо ліміт перед тим, як додати стак ---
            if (tracker.stacks < maxStacks)
            {
                tracker.stacks++;
            }
            else
            {
                Debug.Log("<color=yellow>[Ціль]</color> Досягнуто максимум стаків!");
            }
        }
    }
}