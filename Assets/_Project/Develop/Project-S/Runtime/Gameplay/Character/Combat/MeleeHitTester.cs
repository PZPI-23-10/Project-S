using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public class MeleeHitTester : MonoBehaviour
    {
        [SerializeField] private Transform _attackPoint; // Тут залишається Камера
        [SerializeField] private float _attackDistance = 1.2f; // На скільки метрів ВПЕРЕД вилітає удар
        [SerializeField] private float _hitRadius = 0.4f; // Товщина самого удару (робимо компактним)
        [SerializeField] private LayerMask _targetLayer;

        [Header("Тестовий урон")]
        [SerializeField] private float _testLightDamage = 20f;
        [SerializeField] private float _testPoiseDamage = 5f;

        private float _sphereDisplayTimer;
        private Vector3 _actualHitPosition;

        private void Update()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                PerformTestAttack();
            }

            if (_sphereDisplayTimer > 0)
            {
                _sphereDisplayTimer -= Time.deltaTime;
            }
        }

        private void PerformTestAttack()
        {
            // ФІКС: Тепер беремо позицію камери і ПЛЮСУЄМО вектор напрямку погляду (forward) помножений на дистанцію!
            if (_attackPoint != null)
            {
                _actualHitPosition = _attackPoint.position + (_attackPoint.forward * _attackDistance);
            }
            else
            {
                _actualHitPosition = transform.position + (transform.forward * _attackDistance);
            }

            _sphereDisplayTimer = 0.5f;

            var request = new DamageRequest(gameObject, _testLightDamage, _testPoiseDamage, DamageType.Slashing);

            // Сфера створюється чітко перед носом
            Collider[] hits = Physics.OverlapSphere(_actualHitPosition, _hitRadius, _targetLayer);

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IDamageReceiver>(out var receiver))
                {
                    receiver.ReceiveDamage(request);

                    // Малюємо жовтий лазер від очей (камери) до точки влучання
                    Vector3 rayOrigin = _attackPoint != null ? _attackPoint.position : transform.position;
                    Debug.DrawLine(rayOrigin, hit.transform.position, Color.yellow, 1.5f);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (_sphereDisplayTimer > 0)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
                Gizmos.DrawSphere(_actualHitPosition, _hitRadius);

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_actualHitPosition, _hitRadius);
            }
        }
    }
}