using System;
using Project_S.Runtime.Gameplay.Character.Interaction;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Portals
{
    [DisallowMultipleComponent]
    public class BossPortal : MonoBehaviour, IInteractable, IInteractionActionText
    {
        [SerializeField] private string _interactionPrompt = "Portal";
        [SerializeField] private string _interactionActionText = "E - Close";
        [SerializeField] private Collider _interactionCollider;
        [SerializeField] private ParticleSystem[] _particleSystems;
        [SerializeField] private GameObject[] _particleRoots;
        [SerializeField] private bool _disableParticleGameObjects = true;
        [SerializeField] private bool _bossDefeated;
        [SerializeField] private bool _closed;

        public event Action<BossPortal> Changed;

        public bool IsBossDefeated => _bossDefeated;
        public bool IsClosed => _closed;

        public string InteractionPrompt
        {
            get
            {
                if (_closed)
                    return $"{DisplayName()} (Closed)";

                if (!_bossDefeated)
                    return $"{DisplayName()} (Sealed)";

                return DisplayName();
            }
        }

        public string InteractionActionText => _closed || !_bossDefeated ? string.Empty : _interactionActionText;

        private void Awake()
        {
            EnsureReferences();
            ApplyStateToScene();
        }

        private void OnEnable()
        {
            EnsureReferences();
            ApplyStateToScene();
        }

        private void Reset()
        {
            EnsureReferences();
            ConfigureDefaultInteractionCollider();
            ApplyStateToScene();
        }

        private void OnValidate()
        {
            EnsureReferences();
            ConfigureDefaultInteractionCollider();
            ApplyStateToScene();
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!_bossDefeated || _closed)
                return;

            Close();
        }

        public void MarkBossDefeated()
        {
            if (_bossDefeated)
                return;

            _bossDefeated = true;
            ApplyStateToScene();
            NotifyChanged();
        }

        public void Close()
        {
            bool changed = !_bossDefeated || !_closed;
            _bossDefeated = true;
            _closed = true;
            ApplyStateToScene();

            if (changed)
                NotifyChanged();
        }

        public void RestoreSaveState(bool bossDefeated, bool closed)
        {
            _closed = closed;
            _bossDefeated = bossDefeated || closed;
            ApplyStateToScene();
            NotifyChanged();
        }

        private void EnsureReferences()
        {
            if (_interactionCollider == null)
                _interactionCollider = GetComponent<Collider>();

            if (_particleSystems == null || _particleSystems.Length == 0)
                _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        private void ConfigureDefaultInteractionCollider()
        {
            if (_interactionCollider == null)
                return;

            _interactionCollider.isTrigger = true;

            if (_interactionCollider is BoxCollider box)
            {
                box.center = new Vector3(0f, 2f, 0f);
                box.size = new Vector3(5f, 4f, 2f);
            }
        }

        private void ApplyStateToScene()
        {
            if (_interactionCollider != null)
                _interactionCollider.enabled = _bossDefeated && !_closed;

            if (_closed)
                DisablePortalParticles();
            else
                EnablePortalParticles();
        }

        private void DisablePortalParticles()
        {
            EnsureReferences();

            if (_particleSystems != null)
            {
                for (int i = 0; i < _particleSystems.Length; i++)
                {
                    ParticleSystem particle = _particleSystems[i];
                    if (particle == null)
                        continue;

                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    if (_disableParticleGameObjects && particle.gameObject != gameObject)
                        particle.gameObject.SetActive(false);
                }
            }

            if (_particleRoots == null)
                return;

            for (int i = 0; i < _particleRoots.Length; i++)
            {
                if (_particleRoots[i] != null)
                    _particleRoots[i].SetActive(false);
            }
        }

        private void EnablePortalParticles()
        {
            EnsureReferences();

            if (_particleRoots != null)
            {
                for (int i = 0; i < _particleRoots.Length; i++)
                {
                    if (_particleRoots[i] != null)
                        _particleRoots[i].SetActive(true);
                }
            }

            if (_particleSystems == null)
                return;

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particle = _particleSystems[i];
                if (particle == null)
                    continue;

                if (_disableParticleGameObjects && particle.gameObject != gameObject)
                    particle.gameObject.SetActive(true);

                if (Application.isPlaying && particle.gameObject.activeInHierarchy)
                    particle.Play(true);
            }
        }

        private string DisplayName()
        {
            return string.IsNullOrWhiteSpace(_interactionPrompt) ? name : _interactionPrompt;
        }

        private void NotifyChanged()
        {
            Changed?.Invoke(this);
        }
    }
}
