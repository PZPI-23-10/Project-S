using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Combat;

[CreateAssetMenu(fileName = "MaceRage", menuName = "Project-S/Abilities/Actives/MaceRage")]
public class MaceRageAbility : WeaponActiveData
{
    [Header("Налаштування Люті (Берсерк)")]
    public float baseDuration = 8f;              // Скільки секунд триває лють
    public float attackSpeedMultiplier = 1.5f;   // У скільки разів швидше б'ємо (1.5 = на 50% швидше)

    [Header("Нагороди за вбивство")]
    public float durationExtension = 3f;         // Скільки секунд додаємо за вбивство
    public float staminaRestore = 25f;           // Скільки стаміни відновлюємо

    [Header("Ефекти")]
    public ParticleSystem rageAuraPrefab;        // Червона аура навколо гравця

    public override bool ResetChargeOnUse => true;

    public override void ExecuteHeavyAbility(CombatController combatCtrl, Animator anim, GameObject weaponModel)
    {
        // 1. Шукаємо, чи вже є на гравці бафф (щоб не вішати два одночасно)
        MaceRageBuff buff = combatCtrl.gameObject.GetComponent<MaceRageBuff>();

        // 2. Якщо немає - додаємо
        if (buff == null)
        {
            buff = combatCtrl.gameObject.AddComponent<MaceRageBuff>();
        }

        // 3. Запускаємо/Оновлюємо бафф
        buff.Setup(this, anim, combatCtrl.gameObject);

        Debug.Log("<color=red>[Булава]</color> ЛЮТЬ АКТИВОВАНА!");
    }
}

// =========================================================================
// Цей компонент вішається на гравця тимчасово і сам себе знищує, коли вийде час
// =========================================================================
public class MaceRageBuff : MonoBehaviour
{
    private float _timeLeft;
    private float _attackSpeed;
    private float _staminaGain;
    private float _timeGain;

    private Animator _anim;
    private ParticleSystem _currentAura;

    public void Setup(MaceRageAbility data, Animator anim, GameObject player)
    {
        // Отримуємо значення з налаштувань здібності
        _timeLeft = data.baseDuration;
        _attackSpeed = data.attackSpeedMultiplier;
        _staminaGain = data.staminaRestore;
        _timeGain = data.durationExtension;
        _anim = anim;

        // ПРИСКОРЮЄМО АНІМАЦІЇ АТАК
        if (_anim != null)
        {
            _anim.SetFloat("AttackSpeed", _attackSpeed);
        }

        // Спавним ефект аури, якщо є, і прикріплюємо до гравця
        if (data.rageAuraPrefab != null && _currentAura == null)
        {
            _currentAura = Instantiate(data.rageAuraPrefab, player.transform.position, Quaternion.identity, player.transform);
        }
    }

    private void Update()
    {
        if (_timeLeft > 0)
        {
            _timeLeft -= Time.deltaTime;

            // Якщо час вийшов - зупиняємо Лють
            if (_timeLeft <= 0)
            {
                EndRage();
            }
        }
    }

    // ЦЮ ФУНКЦІЮ МИ БУДЕМО ВИКЛИКАТИ, КОЛИ ВОРОГ ПОМИРАЄ
    public void OnEnemyKilled()
    {
        // Додаємо час
        _timeLeft += _timeGain;

        // TODO: ТУТ ТРЕБА ВІДНОВИТИ СТАМІНУ! 
        // Наприклад: GetComponent<StatsController>().AddStamina(_staminaGain);

        Debug.Log($"<color=red>[Берсерк]</color> Ворога вбито! Час подовжено. Залишилось: {_timeLeft:F1} сек.");
    }

    private void EndRage()
    {
        // Повертаємо швидкість атаки в норму
        if (_anim != null)
        {
            _anim.SetFloat("AttackSpeed", 1f);
        }

        // Знищуємо візуал
        if (_currentAura != null)
        {
            Destroy(_currentAura.gameObject);
        }

        Debug.Log("<color=gray>[Булава]</color> Лють закінчилась.");

        // Видаляємо цей скрипт з гравця
        Destroy(this);
    }
}