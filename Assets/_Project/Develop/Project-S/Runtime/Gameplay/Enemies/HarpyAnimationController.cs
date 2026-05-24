using System.Collections.Generic;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Enemies
{
    public class HarpyAnimationController : MonoBehaviour
    {
        private static readonly int IsFlyingHash = Animator.StringToHash("IsFlying");
        private static readonly int DiveHash = Animator.StringToHash("Dive");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int DieHash = Animator.StringToHash("Die");

        private const string FlyState = "Fly";
        private const string SwoopState = "Swoop";
        private const string WindupState = "Glide";
        private const string AttackState = "JumpClawsAttack";
        private const string DeathState = "Death";

        [SerializeField] private FlyingEnemyController _controller;
        [SerializeField] private EnemyMeleeAttack _meleeAttack;
        [SerializeField] private EnemyHealth _health;
        [SerializeField] private Animator _animator;

        private readonly HashSet<int> _parameters = new HashSet<int>();
        private FlyingEnemyState _lastState;
        private int _currentStateHash;
        private bool _dead;

        private void Awake()
        {
            ResolveReferences();
            CacheParameters();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (_meleeAttack != null)
                _meleeAttack.AttackStarted += OnAttackStarted;

            if (_health != null)
                _health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_meleeAttack != null)
                _meleeAttack.AttackStarted -= OnAttackStarted;

            if (_health != null)
                _health.Died -= OnDied;
        }

        private void Update()
        {
            if (_animator == null || _dead)
                return;

            SetBool(IsFlyingHash, true);

            var state = _controller != null ? _controller.CurrentState : FlyingEnemyState.Hover;
            if (state != _lastState)
            {
                if (state == FlyingEnemyState.Dive)
                    SetTrigger(DiveHash);

                PlayStateFor(state);
                _lastState = state;
                return;
            }

            if (_currentStateHash == 0)
                PlayStateFor(state);
        }

        public void Configure(
            FlyingEnemyController controller,
            EnemyMeleeAttack meleeAttack,
            EnemyHealth health,
            Animator animator)
        {
            _controller = controller;
            _meleeAttack = meleeAttack;
            _health = health;
            _animator = animator;
            _lastState = _controller != null ? _controller.CurrentState : FlyingEnemyState.Hover;

            ResolveReferences();
            CacheParameters();
            PlayState(FlyState, 0f);
        }

        private void ResolveReferences()
        {
            if (_controller == null)
                _controller = GetComponent<FlyingEnemyController>();

            if (_meleeAttack == null)
                _meleeAttack = GetComponent<EnemyMeleeAttack>();

            if (_health == null)
                _health = GetComponent<EnemyHealth>();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            if (_animator != null)
                _animator.applyRootMotion = false;
        }

        private void CacheParameters()
        {
            _parameters.Clear();

            if (_animator == null || _animator.runtimeAnimatorController == null)
                return;

            foreach (var parameter in _animator.parameters)
                _parameters.Add(parameter.nameHash);
        }

        private void PlayStateFor(FlyingEnemyState state)
        {
            switch (state)
            {
                case FlyingEnemyState.Dive:
                    PlayState(SwoopState, 0.08f);
                    break;
                case FlyingEnemyState.Windup:
                    PlayState(WindupState, 0.08f);
                    break;
                case FlyingEnemyState.Attack:
                    PlayState(AttackState, 0.03f);
                    break;
                case FlyingEnemyState.Hover:
                case FlyingEnemyState.Retreat:
                default:
                    PlayState(FlyState, 0.12f);
                    break;
            }
        }

        private void PlayState(string stateName, float transitionDuration)
        {
            if (_animator == null)
                return;

            int hash = Animator.StringToHash(stateName);
            if (_currentStateHash == hash)
                return;

            _currentStateHash = hash;
            if (transitionDuration > 0f)
                _animator.CrossFade(hash, transitionDuration);
            else
                _animator.Play(hash);
        }

        private void OnAttackStarted(EnemyMeleeAttack attack)
        {
            SetTrigger(AttackHash);
            PlayState(AttackState, 0.03f);
        }

        private void OnDied(EnemyHealth health)
        {
            _dead = true;
            SetBool(IsFlyingHash, false);
            SetTrigger(DieHash);
            PlayState(DeathState, 0.05f);
        }

        private void SetBool(int hash, bool value)
        {
            if (_animator != null && _parameters.Contains(hash))
                _animator.SetBool(hash, value);
        }

        private void SetTrigger(int hash)
        {
            if (_animator != null && _parameters.Contains(hash))
                _animator.SetTrigger(hash);
        }
    }
}
