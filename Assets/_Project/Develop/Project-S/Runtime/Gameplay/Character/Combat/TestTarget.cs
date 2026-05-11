using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public class TestTarget : MonoBehaviour, IDamageReceiver
    {
        [SerializeField] private float _health = 100f;

        public void ReceiveDamage(DamageRequest request)
        {
            if (_health <= 0f) return;

            _health -= request.HealthDamage;

            Debug.Log($"<color=red>[УДАР]</color> Сфера '{name}' влучена! Знято ХП: {request.HealthDamage}. Залишилось: {_health}");

            if (TryGetComponent<MeshRenderer>(out var meshRenderer))
            {
                meshRenderer.material.color = Color.red;
            }

            if (_health <= 0f)
            {
                Debug.Log($"<color=black>[ЗНИЩЕНО]</color> Сфера '{name}' знищена!");
                Destroy(gameObject, 0.2f);
            }
        }
    }
}