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

        // Список, щоб не вдарити одного й того ж ворога двічі за один помах
        private HashSet<Collider> _alreadyHit = new HashSet<Collider>();

        // Налаштовуємо зброю (передаємо паспорт і власника)
        public void Setup(WeaponItemData data, GameObject attacker)
        {
            _weaponData = data;
            _attacker = attacker;

            Collider col = GetComponent<Collider>();
            col.isTrigger = true; // Робимо колайдер тригером, щоб він не відштовхував фізичні об'єкти
        }

        // ВМИКАЄМО ЛЕЗО (викликається, коли починається удар)
        public void StartHitDetection()
        {
            _isHitboxActive = true;
            _alreadyHit.Clear(); // Очищаємо список пам'яті для нового удару
        }

        // ВИМИКАЄМО ЛЕЗО (викликається, коли удар завершився)
        public void StopHitDetection()
        {
            _isHitboxActive = false;
            _alreadyHit.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            // 1. Якщо ми зараз не махаємо мечем — лезо безпечне, ігноруємо торкання
            if (!_isHitboxActive || _weaponData == null) return;

            // 2. Якщо торкнулися самі себе — ігноруємо
            if (other.gameObject == _attacker) return;

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

                if (_weaponData.DamageProfile.Count > 0)
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