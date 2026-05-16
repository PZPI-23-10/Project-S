using System.Collections.Generic;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    [RequireComponent(typeof(Collider))]
    public class MeleeHitTester : MonoBehaviour
    {
        private WeaponItemData _weaponData;
        private GameObject _attacker;
        private bool _isHitboxActive = false;
        private Collider _collider;

        // Список, щоб не вдарити одного й того ж ворога двічі за один помах
        private HashSet<Collider> _alreadyHit = new HashSet<Collider>();

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            // На старті гри хітбокс фізично вимкнений, щоб нікого не чіпляти
            if (_collider != null) _collider.enabled = false;
        }

        // Налаштовуємо зброю (передаємо паспорт і власника)
        public void Setup(WeaponItemData data, GameObject attacker)
        {
            _weaponData = data;
            _attacker = attacker;

            if (_collider != null) _collider.isTrigger = true;
        }

        // ВМИКАЄМО ЛЕЗО (викликається з CombatController під час удару)
        public void StartHitDetection()
        {
            _isHitboxActive = true;
            _alreadyHit.Clear();
            // ФІЗИЧНО вмикаємо колайдер! 
            if (_collider != null) _collider.enabled = true;
        }

        // ВИМИКАЄМО ЛЕЗО (викликається через 0.2 сек після початку удару)
        public void StopHitDetection()
        {
            _isHitboxActive = false;
            _alreadyHit.Clear();
            // ФІЗИЧНО вимикаємо колайдер
            if (_collider != null) _collider.enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Перевіряємо, чи є взагалі фізичний контакт
            Debug.Log($"<color=yellow>[ФІЗИКА]</color> Хітбокс торкнувся об'єкта: {other.gameObject.name}");

            // 1. Перевірка на активність та наявність даних
            if (!_isHitboxActive)
            {
                Debug.Log("<color=red>[БЛОК]</color> Хітбокс торкнувся, але _isHitboxActive = false!");
                return;
            }
            if (_weaponData == null)
            {
                Debug.Log("<color=red>[БЛОК]</color> Хітбокс торкнувся, але _weaponData = null!");
                return;
            }

            // 2. ЗАХИСТ ВІД САМОВЛУЧАННЯ (якщо колайдер належить нам - ігноруємо)
            if (other.transform.root == _attacker.transform.root) return;

            // 3. Якщо вже вдарили цю ціль у цьому замаху — не б'ємо ще раз
            if (_alreadyHit.Contains(other)) return;

            // 4. Шукаємо у того, кого торкнулися, компонент IDamageReceiver
            IDamageReceiver receiver = other.GetComponentInParent<IDamageReceiver>();
            if (receiver != null)
            {
                _alreadyHit.Add(other); // Записуємо ціль у "чорний список" цього удару

                // Рахуємо весь урон зі списку DamageProfile
                float totalDamage = 0f;
                DamageType primaryType = DamageType.Blunt; // Тип за замовчуванням

                if (_weaponData.DamageProfile != null && _weaponData.DamageProfile.Count > 0)
                {
                    foreach (var dmgInstance in _weaponData.DamageProfile)
                    {
                        totalDamage += dmgInstance.Amount;
                    }
                    primaryType = _weaponData.DamageProfile[0].Type; // Для DamageRequest беремо перший тип з масиву
                }

                // Створюємо Запит на урон
                DamageRequest request = new DamageRequest(
                    _attacker,
                    totalDamage,
                    _weaponData.PoiseDamage,
                    primaryType
                );

                // Відправляємо урон цілі!
                receiver.ReceiveDamage(request);

                Debug.Log($"<color=orange>[HitTester]</color> ВЛУЧИЛИ по {other.name}! Нанесено {totalDamage} урону.");
            }
        }
    }
}