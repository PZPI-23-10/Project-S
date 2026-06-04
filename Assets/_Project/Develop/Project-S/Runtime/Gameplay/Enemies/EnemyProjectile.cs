using Project_S.Runtime.Gameplay.Character.Combat;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Enemies
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyProjectile : MonoBehaviour
    {
        private const float EnvironmentCollisionGraceSeconds = 0.12f;
        private const float IntendedTargetFallbackRadius = 0.55f;

        private GameObject _source;
        private Transform _intendedTarget;
        private LayerMask _targetLayers = ~0;
        private float _healthDamage;
        private float _poiseDamage;
        private DamageType _damageType = DamageType.Piercing;
        private float _radius = 0.08f;
        private float _age;
        private Vector3 _previousPosition;
        private bool _hasHit;

        public void Configure(
            GameObject source,
            LayerMask targetLayers,
            float healthDamage,
            float poiseDamage,
            DamageType damageType,
            float lifetime,
            float radius = 0.08f,
            Transform intendedTarget = null)
        {
            _source = source;
            _intendedTarget = intendedTarget;
            _targetLayers = targetLayers;
            _healthDamage = Mathf.Max(0f, healthDamage);
            _poiseDamage = Mathf.Max(0f, poiseDamage);
            _damageType = damageType;
            _radius = Mathf.Max(0.01f, radius);
            _previousPosition = transform.position;

            Destroy(gameObject, Mathf.Max(0.01f, lifetime));
        }

        private void FixedUpdate()
        {
            if (_hasHit)
                return;

            _age += Time.fixedDeltaTime;

            Vector3 currentPosition = transform.position;
            Vector3 delta = currentPosition - _previousPosition;
            float distance = delta.magnitude;

            if (distance > 0.001f)
            {
                if (TryHitIntendedTarget(_previousPosition, currentPosition))
                    return;

                if (Physics.SphereCast(
                        _previousPosition,
                        _radius,
                        delta / distance,
                        out RaycastHit hit,
                        distance,
                        _targetLayers,
                        QueryTriggerInteraction.Ignore))
                {
                    TryHit(hit.collider);
                }
            }

            _previousPosition = currentPosition;
        }

        private void OnTriggerEnter(Collider other)
        {
            TryHit(other);
        }

        private bool TryHitIntendedTarget(Vector3 from, Vector3 to)
        {
            if (_intendedTarget == null)
                return false;

            float radius = Mathf.Max(_radius, IntendedTargetFallbackRadius);
            var hits = Physics.OverlapCapsule(
                from,
                to,
                radius,
                _targetLayers,
                QueryTriggerInteraction.Collide);

            foreach (var hit in hits)
            {
                if (hit == null)
                    continue;

                if (hit.transform.root != _intendedTarget.root && !hit.transform.IsChildOf(_intendedTarget))
                    continue;

                if (TryDamage(hit))
                    return true;
            }

            if (DistanceToSegment(_intendedTarget.position + Vector3.up, from, to) <= radius)
            {
                var receiver = _intendedTarget.GetComponentInParent<IDamageReceiver>();
                if (receiver == null)
                    receiver = _intendedTarget.GetComponentInChildren<IDamageReceiver>();

                if (receiver != null)
                {
                    Damage(receiver);
                    return true;
                }
            }

            return false;
        }

        private void TryHit(Collider other)
        {
            if (_hasHit || other == null)
                return;

            if (_source != null && (other.transform == _source.transform || other.transform.IsChildOf(_source.transform)))
                return;

            if (((1 << other.gameObject.layer) & _targetLayers.value) == 0)
                return;

            if (TryDamage(other))
                return;

            if (other.isTrigger || _age < EnvironmentCollisionGraceSeconds)
                return;

            Destroy(gameObject);
        }

        private bool TryDamage(Collider other)
        {
            var receiver = other.GetComponentInParent<IDamageReceiver>();
            if (receiver == null)
                receiver = other.GetComponentInChildren<IDamageReceiver>();

            if (receiver != null)
            {
                Damage(receiver);
                return true;
            }

            return false;
        }

        private void Damage(IDamageReceiver receiver)
        {
            if (_hasHit || receiver == null)
                return;

            _hasHit = true;
            receiver.ReceiveDamage(new DamageRequest(_source, _healthDamage, _poiseDamage, _damageType));
            Destroy(gameObject);
        }

        private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector3 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= 0.0001f)
                return Vector3.Distance(point, start);

            float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
            return Vector3.Distance(point, start + segment * t);
        }
    }
}
