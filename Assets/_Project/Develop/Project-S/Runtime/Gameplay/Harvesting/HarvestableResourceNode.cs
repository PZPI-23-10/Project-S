using System;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Interaction;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Crafting;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Harvesting
{
    public class HarvestableResourceNode : MonoBehaviour, IDamageReceiver, IHoverableInteractable
    {
        [SerializeField] private ResourceNodeData _data;
        [SerializeField] private float _currentHealth;
        [SerializeField] private bool _createFallbackPresentation = true;
        [SerializeField] private Color _fallbackColor = new Color(0.34f, 0.28f, 0.18f);
        [SerializeField] private Color _hitFeedbackColor = new Color(0.9f, 0.72f, 0.35f);

        private bool _depleted;
        private Renderer _feedbackRenderer;
        private Color _baseFeedbackColor;
        private float _feedbackUntil;
        private float _baseYieldHealth;
        private ResourceWorldHealthBar _healthBar;

        private AudioSource _audioSource;

        private readonly Dictionary<ItemData, float> _yieldFractions = new Dictionary<ItemData, float>();

        public event Action<HarvestableResourceNode> HealthChanged;
        public event Action<HarvestableResourceNode> HarvestCompleted;

        public ResourceNodeData Data => _data;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _data != null ? Mathf.Max(1f, _data.MaxHealth) : 1f;
        public float NormalizedHealth => Mathf.Clamp01(_currentHealth / MaxHealth);
        public bool IsDepleted => _depleted;

        public void Configure(ResourceNodeData data)
        {
            _data = data;
            ResetHealth();
            EnsurePresentation();
        }

        private void Awake()
        {
            ResetHealth();
            EnsurePresentation();

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1f;
                _audioSource.minDistance = 2f;
                _audioSource.maxDistance = 20f;
            }
        }

        private void Update()
        {
            if (_feedbackRenderer != null && Time.time >= _feedbackUntil)
                _feedbackRenderer.material.color = _baseFeedbackColor;
        }

        public void SetHovered(bool isHovered)
        {
            if (this == null || gameObject == null) return;

            if (_healthBar == null)
                _healthBar = GetComponent<ResourceWorldHealthBar>();

            if (_healthBar != null)
                _healthBar.SetHovered(isHovered && !_depleted);
        }

        public void ReceiveDamage(DamageRequest request)
        {
            if (_depleted)
                return;

            if (_data == null)
            {
                Debug.LogWarning($"[Harvesting] {name} ignored damage because ResourceNodeData is missing.", this);
                return;
            }

            float damage = CalculateHarvestDamage(request);
            if (damage <= 0f)
            {
                Debug.Log(
                    $"[Harvesting] {NodeName()} ignored hit from {WeaponName(request)}: calculated harvest damage is 0. " +
                    $"PrimaryType={request.Type}, WeaponTool={WeaponToolName(request)}, PreferredTool={_data.PreferredTool}.",
                    this);
                return;
            }

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            float healthRemoved = Mathf.Max(0f, previousHealth - _currentHealth);
            bool matchingTool = IsMatchingTool(request);

            GrantBaseYield(healthRemoved, request.Source, YieldMultiplier(matchingTool));
            ShowHitFeedback();

            PlayFeedback(matchingTool, request.Source);

            HealthChanged?.Invoke(this);
            Debug.Log($"[Harvesting] {NodeName()} took {damage:F1} harvest damage. HP: {_currentHealth:F1}/{_data.MaxHealth:F1}");

            if (_currentHealth <= 0f)
                CompleteHarvest(request.Source, YieldMultiplier(matchingTool));
        }

        private float CalculateHarvestDamage(DamageRequest request)
        {
            float damage = 0f;
            var profile = request.DamageProfile;
            if (profile != null && profile.Count > 0)
            {
                for (int i = 0; i < profile.Count; i++)
                {
                    var instance = profile[i];
                    if (instance.Amount <= 0f)
                        continue;

                    damage += instance.Amount * _data.GetDamageMultiplier(instance.Type);
                }
            }
            else
            {
                damage = Mathf.Max(0f, request.HealthDamage) * _data.GetDamageMultiplier(request.Type);
            }

            float toolMultiplier = IsMatchingTool(request)
                ? _data.MatchingToolDamageMultiplier
                : _data.MismatchedToolDamageMultiplier;

            return Mathf.Max(0f, damage * Mathf.Max(0f, toolMultiplier));
        }

        private static string WeaponName(DamageRequest request)
        {
            return request.Weapon != null ? request.Weapon.ItemName : "None";
        }

        private static string WeaponToolName(DamageRequest request)
        {
            return request.Weapon != null ? request.Weapon.HarvestTool.ToString() : HarvestToolType.None.ToString();
        }

        private bool IsMatchingTool(DamageRequest request)
        {
            if (_data == null || _data.PreferredTool == HarvestToolType.None)
                return true;

            HarvestToolType usedTool = request.Weapon != null ? request.Weapon.HarvestTool : HarvestToolType.None;
            return usedTool == _data.PreferredTool;
        }

        private float YieldMultiplier(bool matchingTool)
        {
            if (_data == null || matchingTool)
                return 1f;

            return Mathf.Clamp01(_data.MismatchedYieldMultiplier);
        }

        private void GrantBaseYield(float healthRemoved, GameObject source, float yieldMultiplier)
        {
            if (_data.BaseYieldItem == null || _data.HealthPerBaseYield <= 0f || healthRemoved <= 0f)
                return;

            _baseYieldHealth += healthRemoved;
            int thresholds = Mathf.FloorToInt(_baseYieldHealth / _data.HealthPerBaseYield);
            if (thresholds <= 0)
                return;

            _baseYieldHealth -= thresholds * _data.HealthPerBaseYield;
            int amount = thresholds * Mathf.Max(1, _data.BaseYieldAmount);
            GrantItem(_data.BaseYieldItem, amount, source, yieldMultiplier);
        }

        private void CompleteHarvest(GameObject source, float yieldMultiplier)
        {
            _depleted = true;
            SetHovered(false);

            InventoryController inventory = source != null ? source.GetComponentInParent<InventoryController>() : null;
            SoulAshWallet wallet = ResolveWallet(source, inventory);

            if (wallet != null && _data.SoulAshReward > 0)
                wallet.AddReward(_data.SoulAshReward, source);

            if (_data.Drops != null)
            {
                foreach (var drop in _data.Drops)
                    GrantDrop(drop, source, inventory, yieldMultiplier);
            }

            Debug.Log($"[Harvesting] {NodeName()} harvested.");
            HarvestCompleted?.Invoke(this);
            MarkPresentationDepleted();

            if (_data != null && _data.DestructionSound != null)
            {
                AudioSource.PlayClipAtPoint(_data.DestructionSound, transform.position, 1f);
            }

            var depletionHandler = GetComponent<IResourceDepletionHandler>();
            if (depletionHandler != null)
                depletionHandler.HandleResourceDepleted(this);
            else
                DestroyNode();
        }

        private void PlayFeedback(bool isCorrectTool, GameObject attacker)
        {
            if (_data == null) return;

            AudioClip[] clipsToPlay = isCorrectTool ? _data.CorrectToolSounds : _data.WrongToolSounds;

            if (clipsToPlay != null && clipsToPlay.Length > 0 && _audioSource != null)
            {
                AudioClip randomClip = clipsToPlay[UnityEngine.Random.Range(0, clipsToPlay.Length)];
                _audioSource.pitch = UnityEngine.Random.Range(0.85f, 1.15f);
                _audioSource.PlayOneShot(randomClip);
            }

            if (_data.HitVFXPrefab != null)
            {
                Vector3 spawnPos = transform.position + Vector3.up * 1f;

                if (attacker != null)
                {
                    Vector3 directionToAttacker = (attacker.transform.position - transform.position).normalized;
                    spawnPos += directionToAttacker * 0.5f;
                }

                Instantiate(_data.HitVFXPrefab, spawnPos, Quaternion.identity);
            }
        }

        private static SoulAshWallet ResolveWallet(GameObject source, InventoryController inventory)
        {
            if (source != null)
            {
                var wallet = source.GetComponentInParent<SoulAshWallet>();
                if (wallet != null)
                    return wallet;
            }

            return inventory != null ? inventory.GetComponent<SoulAshWallet>() : null;
        }

        private void GrantDrop(ResourceDrop drop, GameObject source, InventoryController inventory, float yieldMultiplier)
        {
            if (drop == null || drop.Item == null || drop.Chance <= 0f)
                return;

            if (drop.Chance < 1f && UnityEngine.Random.value > drop.Chance)
                return;

            int amount = drop.RollAmount();
            if (amount <= 0)
                return;

            GrantItem(drop.Item, amount, source, yieldMultiplier, inventory);
        }

        private void GrantItem(ItemData item, int amount, GameObject source, float yieldMultiplier, InventoryController inventory = null)
        {
            if (item == null || amount <= 0)
                return;

            if (inventory == null && source != null)
                inventory = source.GetComponentInParent<InventoryController>();

            int finalAmount = ScaleYieldAmount(item, amount, yieldMultiplier);
            if (finalAmount <= 0)
                return;

            WorldItemDropUtility.GrantOrDrop(item, finalAmount, inventory, transform.position, "[Harvesting]");
        }

        private int ScaleYieldAmount(ItemData item, int amount, float yieldMultiplier)
        {
            if (amount <= 0 || yieldMultiplier <= 0f)
                return 0;

            if (Mathf.Approximately(yieldMultiplier, 1f))
                return amount;

            float carried = _yieldFractions.TryGetValue(item, out float value) ? value : 0f;
            float scaled = amount * yieldMultiplier + carried;
            int wholeAmount = Mathf.FloorToInt(scaled);
            _yieldFractions[item] = scaled - wholeAmount;
            return wholeAmount;
        }

        private void ResetHealth()
        {
            _currentHealth = MaxHealth;
            _depleted = false;
            _baseYieldHealth = 0f;
            _yieldFractions.Clear();
            HealthChanged?.Invoke(this);
        }

        private string NodeName()
        {
            return _data != null ? _data.NodeName : name;
        }

        private void DestroyNode()
        {
            if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
        }

        private void EnsurePresentation()
        {
            if (_createFallbackPresentation && GetComponent<Collider>() == null)
                gameObject.AddComponent<BoxCollider>();

            _feedbackRenderer = GetComponentInChildren<Renderer>();
            if (_feedbackRenderer == null && _createFallbackPresentation)
            {
                var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "MVP Resource Visual";
                visual.transform.SetParent(transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;

                var visualCollider = visual.GetComponent<Collider>();
                if (visualCollider != null)
                {
                    if (Application.isPlaying)
                        Destroy(visualCollider);
                    else
                        DestroyImmediate(visualCollider);
                }

                _feedbackRenderer = visual.GetComponent<Renderer>();
            }

            if (_feedbackRenderer != null)
            {
                if (_feedbackRenderer.sharedMaterial != null)
                    _baseFeedbackColor = _feedbackRenderer.sharedMaterial.color;
                else
                    _baseFeedbackColor = _fallbackColor;

                if (_feedbackRenderer.sharedMaterial == null)
                    _feedbackRenderer.material.color = _fallbackColor;
            }
        }

        private void ShowHitFeedback()
        {
            if (_feedbackRenderer == null)
                return;

            _feedbackRenderer.material.color = _hitFeedbackColor;
            _feedbackUntil = Time.time + 0.12f;
        }

        private void MarkPresentationDepleted()
        {
            var collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
        }
    }
}