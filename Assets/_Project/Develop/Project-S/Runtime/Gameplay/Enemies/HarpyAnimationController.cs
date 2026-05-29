using System.Collections;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Ambient;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

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
        private const float DeathFallFallbackSeconds = 5f;
        private const float DeathGroundOffset = 0.08f;
        private const float GroundProbeHeight = 25f;
        private const float GroundProbeDistance = 80f;
        private const int GroundLayerMask = 1 << 8;
        private static readonly int DeathStateHash = Animator.StringToHash(DeathState);

        [SerializeField] private FlyingEnemyController _controller;
        [SerializeField] private EnemyMeleeAttack _meleeAttack;
        [SerializeField] private EnemyHealth _health;
        [SerializeField] private AnimalCorpseHarvest _corpseHarvest;
        [SerializeField] private Animator _animator;
        [SerializeField] private AnimationClip _deathClip;
        [SerializeField] private AnimationClip _deathHitGroundClip;

        private readonly HashSet<int> _parameters = new HashSet<int>();
        private PlayableGraph _deathGraph;
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

            StopDeathPlayable();
        }

        private void OnDestroy()
        {
            StopDeathPlayable();
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

        public void Configure(
            FlyingEnemyController controller,
            EnemyMeleeAttack meleeAttack,
            EnemyHealth health,
            Animator animator,
            AnimationClip deathClip,
            AnimationClip deathHitGroundClip)
        {
            _deathClip = deathClip;
            _deathHitGroundClip = deathHitGroundClip;
            Configure(controller, meleeAttack, health, animator);
        }

        private void ResolveReferences()
        {
            if (_controller == null)
                _controller = GetComponent<FlyingEnemyController>();

            if (_meleeAttack == null)
                _meleeAttack = GetComponent<EnemyMeleeAttack>();

            if (_health == null)
                _health = GetComponent<EnemyHealth>();

            if (_corpseHarvest == null)
                _corpseHarvest = GetComponent<AnimalCorpseHarvest>();

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
            ResolveReferences();
            _dead = true;
            SetBool(IsFlyingHash, false);
            SetTrigger(DieHash);

            if (_corpseHarvest != null)
                StartCoroutine(CompleteCorpseAfterDeathAnimation());
            else
                PlayState(DeathState, 0.05f);
        }

        private IEnumerator CompleteCorpseAfterDeathAnimation()
        {
            if (_animator == null)
            {
                _corpseHarvest.CompleteExternalDeathPose(applyScriptedPose: false);
                yield break;
            }

            if (_deathClip != null)
                yield return PlayDeathClip(_deathClip, "Death");
            else
                yield return PlayAnimatorDeathState();

            if (_deathHitGroundClip != null)
                yield return PlayDeathClip(_deathHitGroundClip, "DeathHitGround");

            if (_corpseHarvest != null)
                _corpseHarvest.CompleteExternalDeathPose(applyScriptedPose: false);
        }

        private IEnumerator PlayAnimatorDeathState()
        {
            Vector3 startPosition = transform.position;
            Vector3 endPosition = SampleGround(startPosition) + Vector3.up * DeathGroundOffset;
            float elapsed = 0f;

            PlayState(DeathState, 0.05f);
            yield return null;

            while (_animator != null)
            {
                var state = _animator.GetCurrentAnimatorStateInfo(0);
                bool deathStateReached = state.IsName(DeathState) || state.shortNameHash == DeathStateHash;
                float animationProgress = deathStateReached ? Mathf.Clamp01(state.normalizedTime) : 0f;
                float fallbackProgress = Mathf.Clamp01(elapsed / DeathFallFallbackSeconds);
                float progress = Mathf.Max(animationProgress, fallbackProgress);
                transform.position = Vector3.Lerp(startPosition, endPosition, Mathf.SmoothStep(0f, 1f, progress));

                if (deathStateReached && !_animator.IsInTransition(0) && state.normalizedTime >= 1f)
                {
                    transform.position = endPosition;
                    yield break;
                }

                elapsed += Time.deltaTime;
                if (elapsed >= DeathFallFallbackSeconds)
                {
                    transform.position = endPosition;
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator PlayDeathClip(AnimationClip clip, string outputName)
        {
            StopDeathPlayable();
            _animator.enabled = true;

            _deathGraph = PlayableGraph.Create($"{name}_{outputName}Animation");
            _deathGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            var clipPlayable = AnimationClipPlayable.Create(_deathGraph, clip);
            clipPlayable.SetApplyFootIK(false);
            clipPlayable.SetApplyPlayableIK(false);
            clipPlayable.SetDuration(clip.length);

            var output = AnimationPlayableOutput.Create(_deathGraph, outputName, _animator);
            output.SetSourcePlayable(clipPlayable);

            _deathGraph.Play();
            _deathGraph.Evaluate(0f);

            float remaining = Mathf.Max(0.01f, clip.length);
            while (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                yield return null;
            }

            StopDeathPlayable();
        }

        private void StopDeathPlayable()
        {
            if (_deathGraph.IsValid())
                _deathGraph.Destroy();
        }

        private static Vector3 SampleGround(Vector3 position)
        {
            Vector3 origin = position + Vector3.up * GroundProbeHeight;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, GroundProbeDistance, GroundLayerMask, QueryTriggerInteraction.Ignore))
                return hit.point;

            position.y = 0f;
            return position;
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
