using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Combat;

[RequireComponent(typeof(Rigidbody))]
public class ThrowingKnifeProjectile : MonoBehaviour
{
    [Header("Ефекти")]
    public ParticleSystem hitEffect; // Кров або іскри при влучанні
    public float lifeTime = 5f;      // Через скільки секунд ніж зникне, якщо нікуди не влучить

    private GameObject _attacker;
    private List<DamageInstance> _damageProfile;
    private float _poiseDamage;
    private bool _hasHit = false;

    // Цю функцію викликає наша ліва рука в момент кидка
    public void Setup(GameObject attacker, List<DamageInstance> damage, float poise, float speed)
    {
        _attacker = attacker;
        _damageProfile = damage;
        _poiseDamage = poise;

        // Налаштовуємо фізику польоту
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Щоб не пролітав крізь стіни на великій швидкості
        rb.useGravity = false; // Летить прямо. Якщо хочеш балістику — постав true
        rb.velocity = transform.forward * speed;

        // Запобіжник: видаляємо об'єкт через 5 секунд, щоб не засмічувати пам'ять
        Destroy(gameObject, lifeTime);
    }

    // Коли ніж у щось врізається (повинен мати Collider з увімкненим IsTrigger)
    private void OnTriggerEnter(Collider other)
    {
        // Ігноруємо самого себе та повторні влучання
        if (_hasHit || other.gameObject == _attacker || other.transform.root == _attacker.transform.root)
            return;

        _hasHit = true; // Запобігаємо подвійному урону

        // Шукаємо інтерфейс отримання урону (чи це ворог?)
        IDamageReceiver target = other.GetComponentInParent<IDamageReceiver>();
        if (target != null)
        {
            // Створюємо запит на урон
            var request = new DamageRequest(_attacker, _damageProfile, _poiseDamage, null);
            target.ReceiveDamage(request);

            Debug.Log($"<color=cyan>[Метальний ніж]</color> Влучили по {other.name}!");
        }

        // Відтворюємо ефект влучання (якщо є)
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, transform.rotation);
        }

        // Знищуємо сам ніж
        Destroy(gameObject);
    }
}