using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

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
        [SerializeField] private AnimationClip _deathClip;
        [SerializeField] private AnimationClip _idleClip;
        [SerializeField] private AnimationClip _walkClip;
        [SerializeField] private AnimationClip _hitReactionClip;
        [SerializeField] private AnimationClip[] _hitReactionClips;
        [SerializeField] private AnimationClip _attackClip;

        private readonly HashSet<int> _parameters = new HashSet<int>();
        private PlayableGraph _locomotionGraph;
        private AnimationClipPlayable _locomotionPlayable;
        private AnimationClip _currentLocomotionClip;
        private PlayableGraph _hitReactionGraph;
        private PlayableGraph _attackGraph;
        private PlayableGraph _deathGraph;
        private Quaternion _baseLocalRotation;
        private Vector3 _baseLocalPosition;
        private float _deathGroundY;
        private float _swingDuration = 0.45f;
        private float _swingRemaining;
        private float _hitReactionRemaining;
        private float _attackClipRemaining;
        private bool _isDead;

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
            {
                _health.Damaged += OnDamaged;
                _health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            StopLocomotionPlayable();
            StopHitReactionPlayable();
            StopAttackPlayable();
            StopDeathPlayable();

            if (_meleeAttack != null)
            {
                _meleeAttack.AttackStarted -= OnAttackStarted;
                _meleeAttack.AttackResolved -= OnAttackResolved;
            }

            if (_health != null)
            {
                _health.Damaged -= OnDamaged;
                _health.Died -= OnDied;
            }
        }

        private void OnDestroy()
        {
            StopLocomotionPlayable();
            StopHitReactionPlayable();
            StopAttackPlayable();
            StopDeathPlayable();
        }

        private void Update()
        {
            UpdateHitReactionPlayable(Time.deltaTime);
            UpdateAttackPlayable(Time.deltaTime);
            UpdateAnimatorParameters();
            UpdateLocomotionPlayable();
        }

        private void LateUpdate()
        {
            UpdateProceduralSwing(Time.deltaTime);

            if (_isDead)
                KeepDeadVisualRootAnchored();
            else
                KeepAliveVisualRootAnchored();
        }

        public void Configure(
            EnemyController controller,
            EnemyMeleeAttack meleeAttack,
            EnemyHealth health,
            Transform visualRoot,
            Animator animator,
            AnimationClip deathClip = null,
            AnimationClip idleClip = null,
            AnimationClip walkClip = null,
            AnimationClip hitReactionClip = null,
            AnimationClip attackClip = null)
        {
            _controller = controller;
            _meleeAttack = meleeAttack;
            _health = health;
            _visualRoot = visualRoot;
            _animator = animator;
            _deathClip = deathClip;
            _idleClip = idleClip;
            _walkClip = walkClip;
            _hitReactionClip = hitReactionClip;
            _attackClip = attackClip;

            ResolveReferences();
            CacheAnimatorParameters();
            CacheBasePose();
        }

        public void ConfigureHitReactionClips(AnimationClip[] hitReactionClips)
        {
            _hitReactionClips = hitReactionClips;
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
            {
                _baseLocalRotation = _visualRoot.localRotation;
                _baseLocalPosition = _visualRoot.localPosition;
            }
        }

        private void UpdateAnimatorParameters()
        {
            if (_animator == null || _isDead)
                return;

            bool isMoving = _controller != null && _controller.IsMoving;
            float speed = isMoving ? 1f : 0f;

            SetBool(IsMovingHash, isMoving);
            SetFloat(SpeedHash, speed, 0.12f, Time.deltaTime);
            SetFloat(ForwardHash, speed, 0.12f, Time.deltaTime);
            SetFloat(TurnHash, 0f, 0.08f, Time.deltaTime);
            SetBool(OnGroundHash, true);
        }

        private void UpdateLocomotionPlayable()
        {
            if (_animator == null || _isDead || _hitReactionGraph.IsValid() || _attackGraph.IsValid())
            {
                StopLocomotionPlayable();
                return;
            }

            bool shouldWalk = _controller != null && _controller.IsMoving;
            var clip = shouldWalk ? _walkClip : _idleClip;
            if (clip == null)
            {
                StopLocomotionPlayable();
                return;
            }

            if (!_locomotionGraph.IsValid() || _currentLocomotionClip != clip)
                PlayLocomotionClip(clip, shouldWalk ? "Walk" : "Idle");

            if (_locomotionGraph.IsValid() && _locomotionPlayable.IsValid() && clip.length > 0.01f && _locomotionPlayable.GetTime() >= clip.length)
                _locomotionPlayable.SetTime(0d);
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
            if (_hitReactionGraph.IsValid())
                return;

            var clip = attack != null && attack.CurrentAttackClip != null
                ? attack.CurrentAttackClip
                : _attackClip;
            _swingDuration = clip != null && clip.length > 0.01f
                ? clip.length
                : (attack != null ? attack.WindupDuration : 0.45f);

            if (attack != null)
                attack.OverrideCurrentWindupFromClip(_swingDuration);

            StopLocomotionPlayable();

            if (PlayAttackClip(clip))
                _swingRemaining = 0f;
            else
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
            _isDead = true;
            _swingRemaining = 0f;
            _deathGroundY = transform.position.y;

            if (_visualRoot != null)
            {
                _visualRoot.localRotation = _baseLocalRotation;
                _visualRoot.localPosition = _baseLocalPosition;
            }

            StopAttackPlayable();
            StopHitReactionPlayable();
            StopLocomotionPlayable();

            if (!PlayDeathClip())
                SetTrigger(DieHash);
        }

        private void OnDamaged(EnemyHealth health)
        {
            if (_isDead)
                return;

            _swingRemaining = 0f;
            var hitReactionClip = SelectHitReactionClip();
            float duration = hitReactionClip != null && hitReactionClip.length > 0.01f
                ? hitReactionClip.length
                : 0.35f;

            if (_controller != null)
                _controller.StunFor(duration);

            StopAttackPlayable();
            StopLocomotionPlayable();

            if (!PlayHitReactionClip(hitReactionClip))
                _hitReactionRemaining = duration;
        }

        private bool PlayAttackClip(AnimationClip clip)
        {
            if (_animator == null || clip == null)
                return false;

            StopAttackPlayable();
            _animator.enabled = true;

            _attackGraph = PlayableGraph.Create($"{name}_AttackAnimation");
            _attackGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            var clipPlayable = AnimationClipPlayable.Create(_attackGraph, clip);
            clipPlayable.SetApplyFootIK(false);
            clipPlayable.SetApplyPlayableIK(false);
            clipPlayable.SetDuration(clip.length);

            var output = AnimationPlayableOutput.Create(_attackGraph, "Attack", _animator);
            output.SetSourcePlayable(clipPlayable);

            _attackClipRemaining = clip.length;
            _attackGraph.Play();
            _attackGraph.Evaluate(0f);
            return true;
        }

        private bool PlayHitReactionClip(AnimationClip clip)
        {
            if (_animator == null || clip == null)
                return false;

            StopHitReactionPlayable();
            _animator.enabled = true;

            _hitReactionGraph = PlayableGraph.Create($"{name}_HitReactionAnimation");
            _hitReactionGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            var clipPlayable = AnimationClipPlayable.Create(_hitReactionGraph, clip);
            clipPlayable.SetApplyFootIK(false);
            clipPlayable.SetApplyPlayableIK(false);
            clipPlayable.SetDuration(clip.length);

            var output = AnimationPlayableOutput.Create(_hitReactionGraph, "HitReaction", _animator);
            output.SetSourcePlayable(clipPlayable);

            _hitReactionRemaining = clip.length;
            _hitReactionGraph.Play();
            _hitReactionGraph.Evaluate(0f);
            return true;
        }

        private AnimationClip SelectHitReactionClip()
        {
            if (_hitReactionClips != null && _hitReactionClips.Length > 0)
            {
                var validClips = new List<AnimationClip>();
                foreach (var clip in _hitReactionClips)
                {
                    if (clip != null)
                        validClips.Add(clip);
                }

                if (validClips.Count > 0)
                    return validClips[Random.Range(0, validClips.Count)];
            }

            return _hitReactionClip;
        }

        private void PlayLocomotionClip(AnimationClip clip, string outputName)
        {
            if (_animator == null || clip == null)
                return;

            StopLocomotionPlayable();
            _animator.enabled = true;

            _locomotionGraph = PlayableGraph.Create($"{name}_{outputName}Animation");
            _locomotionGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            _locomotionPlayable = AnimationClipPlayable.Create(_locomotionGraph, clip);
            _locomotionPlayable.SetApplyFootIK(false);
            _locomotionPlayable.SetApplyPlayableIK(false);
            _locomotionPlayable.SetDuration(clip.length);

            var output = AnimationPlayableOutput.Create(_locomotionGraph, outputName, _animator);
            output.SetSourcePlayable(_locomotionPlayable);

            _currentLocomotionClip = clip;
            _locomotionGraph.Play();
            _locomotionGraph.Evaluate(0f);
        }

        private void UpdateAttackPlayable(float deltaTime)
        {
            if (!_attackGraph.IsValid() || _isDead)
                return;

            _attackClipRemaining -= Mathf.Max(0f, deltaTime);
            if (_attackClipRemaining <= 0f)
                StopAttackPlayable();
        }

        private void UpdateHitReactionPlayable(float deltaTime)
        {
            if (!_hitReactionGraph.IsValid() || _isDead)
                return;

            _hitReactionRemaining -= Mathf.Max(0f, deltaTime);
            if (_hitReactionRemaining <= 0f)
                StopHitReactionPlayable();
        }

        private bool PlayDeathClip()
        {
            if (_animator == null)
            {
                Debug.LogWarning($"[Skeleton] Cannot play death animation for '{name}': Animator is missing.");
                return false;
            }

            if (_deathClip == null)
            {
                Debug.LogWarning($"[Skeleton] Cannot play death animation for '{name}': death clip is missing.");
                return false;
            }

            StopDeathPlayable();
            _animator.enabled = true;

            _deathGraph = PlayableGraph.Create($"{name}_DeathAnimation");
            _deathGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            var clipPlayable = AnimationClipPlayable.Create(_deathGraph, _deathClip);
            clipPlayable.SetApplyFootIK(false);
            clipPlayable.SetApplyPlayableIK(false);
            clipPlayable.SetDuration(_deathClip.length);

            var output = AnimationPlayableOutput.Create(_deathGraph, "Death", _animator);
            output.SetSourcePlayable(clipPlayable);

            _deathGraph.Play();
            _deathGraph.Evaluate(0f);
            AnchorVisualRootToGround(_deathGroundY);
            return true;
        }

        private void KeepAliveVisualRootAnchored()
        {
            if (_visualRoot == null)
                return;

            var localPosition = _visualRoot.localPosition;
            localPosition.x = _baseLocalPosition.x;
            localPosition.z = _baseLocalPosition.z;
            _visualRoot.localPosition = localPosition;
            AnchorVisualRootToGround(transform.position.y);
        }

        private void KeepDeadVisualRootAnchored()
        {
            if (!_isDead || _visualRoot == null)
                return;

            var localPosition = _visualRoot.localPosition;
            localPosition.x = _baseLocalPosition.x;
            localPosition.z = _baseLocalPosition.z;
            _visualRoot.localPosition = localPosition;
            AnchorVisualRootToGround(_deathGroundY);
        }

        private void AnchorVisualRootToGround(float groundY)
        {
            if (_visualRoot == null)
                return;

            if (!TryGetVisualBounds(out Bounds bounds))
                return;

            float deltaY = groundY - bounds.min.y;
            if (Mathf.Abs(deltaY) < 0.005f)
                return;

            _visualRoot.position += Vector3.up * deltaY;
        }

        private bool TryGetVisualBounds(out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            foreach (var renderer in _visualRoot.GetComponentsInChildren<Renderer>())
            {
                if (renderer == null || !renderer.enabled)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private void StopDeathPlayable()
        {
            if (_deathGraph.IsValid())
                _deathGraph.Destroy();
        }

        private void StopLocomotionPlayable()
        {
            _locomotionPlayable = default;
            _currentLocomotionClip = null;

            if (_locomotionGraph.IsValid())
                _locomotionGraph.Destroy();
        }

        private void StopHitReactionPlayable()
        {
            _hitReactionRemaining = 0f;

            if (_hitReactionGraph.IsValid())
                _hitReactionGraph.Destroy();
        }

        private void StopAttackPlayable()
        {
            _attackClipRemaining = 0f;

            if (_attackGraph.IsValid())
                _attackGraph.Destroy();
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
