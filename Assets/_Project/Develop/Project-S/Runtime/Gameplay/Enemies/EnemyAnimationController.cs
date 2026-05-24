using System.Collections.Generic;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Enemies
{
    public class EnemyAnimationController : MonoBehaviour
    {
        private static readonly int ForwardHash = Animator.StringToHash("Forward");
        private static readonly int TurnHash = Animator.StringToHash("Turn");
        private static readonly int OnGroundHash = Animator.StringToHash("OnGround");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int DieHash = Animator.StringToHash("Die");

        [SerializeField] private EnemyController _controller;
        [SerializeField] private EnemyMeleeAttack _meleeAttack;
        [SerializeField] private EnemyHealth _health;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Animator _animator;

        private readonly HashSet<int> _parameters = new HashSet<int>();
        private Quaternion _baseLocalRotation;
        private float _swingDuration = 0.45f;
        private float _swingRemaining;

        private void Awake()
        {
            ResolveReferences();
            CacheAnimatorParameters();
            CacheBasePose();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (_meleeAttack != null)
            {
                _meleeAttack.AttackStarted += OnAttackStarted;
                _meleeAttack.AttackResolved += OnAttackResolved;
            }

            if (_health != null)
                _health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_meleeAttack != null)
            {
                _meleeAttack.AttackStarted -= OnAttackStarted;
                _meleeAttack.AttackResolved -= OnAttackResolved;
            }

            if (_health != null)
                _health.Died -= OnDied;
        }

        private void Update()
        {
            UpdateAnimatorParameters();
        }

        private void LateUpdate()
        {
            UpdateProceduralSwing(Time.deltaTime);
        }

        public void Configure(
            EnemyController controller,
            EnemyMeleeAttack meleeAttack,
            EnemyHealth health,
            Transform visualRoot,
            Animator animator)
        {
            _controller = controller;
            _meleeAttack = meleeAttack;
            _health = health;
            _visualRoot = visualRoot;
            _animator = animator;

            ResolveReferences();
            CacheAnimatorParameters();
            CacheBasePose();
        }

        private void ResolveReferences()
        {
            if (_controller == null)
                _controller = GetComponent<EnemyController>();

            if (_meleeAttack == null)
                _meleeAttack = GetComponent<EnemyMeleeAttack>();

            if (_health == null)
                _health = GetComponent<EnemyHealth>();

            if (_visualRoot == null)
                _visualRoot = transform.Find("VisualRoot");

            if (_animator == null && _visualRoot != null)
                _animator = _visualRoot.GetComponentInChildren<Animator>();
        }

        private void CacheAnimatorParameters()
        {
            _parameters.Clear();

            if (_animator == null || _animator.runtimeAnimatorController == null)
                return;

            foreach (var parameter in _animator.parameters)
                _parameters.Add(parameter.nameHash);
        }

        private void CacheBasePose()
        {
            if (_visualRoot != null)
                _baseLocalRotation = _visualRoot.localRotation;
        }

        private void UpdateAnimatorParameters()
        {
            if (_animator == null)
                return;

            bool isMoving = _controller != null && _controller.IsMoving;
            float speed = isMoving ? 1f : 0f;

            SetBool(IsMovingHash, isMoving);
            SetFloat(SpeedHash, speed, 0.12f, Time.deltaTime);
            SetFloat(ForwardHash, speed, 0.12f, Time.deltaTime);
            SetFloat(TurnHash, 0f, 0.08f, Time.deltaTime);
            SetBool(OnGroundHash, true);
        }

        private void UpdateProceduralSwing(float deltaTime)
        {
            if (_visualRoot == null || _swingRemaining <= 0f)
                return;

            _swingRemaining = Mathf.Max(0f, _swingRemaining - Mathf.Max(0f, deltaTime));

            float progress = 1f - (_swingRemaining / Mathf.Max(0.01f, _swingDuration));
            float pitch = progress < 0.45f
                ? Mathf.Lerp(0f, -18f, progress / 0.45f)
                : Mathf.Lerp(-18f, 16f, (progress - 0.45f) / 0.55f);
            float roll = Mathf.Sin(progress * Mathf.PI) * 7f;

            if (_swingRemaining <= 0f)
            {
                _visualRoot.localRotation = _baseLocalRotation;
                return;
            }

            _visualRoot.localRotation = _baseLocalRotation * Quaternion.Euler(pitch, 0f, roll);
        }

        private void OnAttackStarted(EnemyMeleeAttack attack)
        {
            _swingDuration = attack != null ? attack.WindupDuration : 0.45f;
            _swingRemaining = _swingDuration;
            SetTrigger(AttackHash);
        }

        private void OnAttackResolved(EnemyMeleeAttack attack)
        {
            if (_visualRoot != null && _swingRemaining <= 0f)
                _visualRoot.localRotation = _baseLocalRotation;
        }

        private void OnDied(EnemyHealth health)
        {
            SetTrigger(DieHash);
        }

        private void SetBool(int hash, bool value)
        {
            if (_animator != null && _parameters.Contains(hash))
                _animator.SetBool(hash, value);
        }

        private void SetFloat(int hash, float value, float dampTime, float deltaTime)
        {
            if (_animator != null && _parameters.Contains(hash))
                _animator.SetFloat(hash, value, dampTime, deltaTime);
        }

        private void SetTrigger(int hash)
        {
            if (_animator != null && _parameters.Contains(hash))
                _animator.SetTrigger(hash);
        }
    }
}
