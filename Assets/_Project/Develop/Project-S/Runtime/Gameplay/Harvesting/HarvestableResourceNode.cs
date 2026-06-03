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
        private Renderer[] _feedbackRenderers;
        private MaterialPropertyBlock _feedbackBlock;
        private float _feedbackUntil;
        private bool _feedbackActive;
        private float _baseYieldHealth;
        private ResourceWorldHealthBar _healthBar;

        private readonly Dictionary<ItemData, float> _yieldFractions = new Dictionary<ItemData, float>();
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
        private static readonly int HueVariationId = Shader.PropertyToID("_HueVariation");

        public event Action<HarvestableResourceNode> HealthChanged;
        public event Action<HarvestableResourceNode> HarvestCompleted;

        public ResourceNodeData Data => _data;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _data != null ? Mathf.Max(1f, _data.MaxHealth) : 1f;
        public float NormalizedHealth => Mathf.Clamp01(_currentHealth / MaxHealth);
        public bool IsDepleted => _depleted;

        public void RestoreSaveState(float currentHealth, bool depleted)
        {
            _depleted = depleted;
            _currentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth);

            if (_depleted || _currentHealth <= 0f)
            {
                _depleted = true;
                _currentHealth = 0f;
                MarkPresentationDepleted();
            }
            else
            {
                var collider = GetComponent<Collider>();
                if (collider != null)
                    collider.enabled = true;
            }

            HealthChanged?.Invoke(this);
        }

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
            enabled = false;
        }

        private void Update()
        {
            if (_feedbackActive && Time.time >= _feedbackUntil)
                ClearHitFeedback();
        }

        public void SetHovered(bool isHovered)
        {
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
                Debug.Log($"[Harvesting] {NodeName()} ignored hit from {WeaponName(request)}: calculated harvest damage is 0.");
                return;
            }

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            float healthRemoved = Mathf.Max(0f, previousHealth - _currentHealth);
            bool matchingTool = IsMatchingTool(request);

            GrantBaseYield(healthRemoved, request.Source, YieldMultiplier(matchingTool));
            ShowHitFeedback();

            // ==========================================
            // ФІКС: Відтворюємо звуки та VFX ресурсу!
            // ==========================================
            PlayHitSoundAndVFX(matchingTool, request.Source);
            // ==========================================

            HealthChanged?.Invoke(this);
            Debug.Log($"[Harvesting] {NodeName()} took {damage:F1} harvest damage. HP: {_currentHealth:F1}/{_data.MaxHealth:F1}");

            if (_currentHealth <= 0f)
                CompleteHarvest(request.Source, YieldMultiplier(matchingTool));
        }

        // ==========================================
        // ДОДАНО: Логіка відтворення звуку та іскор
        // ==========================================
        private void PlayHitSoundAndVFX(bool matchingTool, GameObject attacker)
        {
            if (_data == null) return;

            // 1. Граємо звук (Вибираємо випадковий з масиву)
            AudioClip[] soundArray = matchingTool ? _data.CorrectToolSounds : _data.WrongToolSounds;
            if (soundArray != null && soundArray.Length > 0)
            {
                AudioClip clip = soundArray[UnityEngine.Random.Range(0, soundArray.Length)];
                if (clip != null)
                {
                    // Відтворюємо звук прямо в точці, де стоїть ресурс
                    AudioSource.PlayClipAtPoint(clip, transform.position, 1f);
                }
            }

            // 2. Спавнимо VFX (іскри/тріски деревини)
            if (_data.HitVFXPrefab != null)
            {
                // Якщо є атакуючий, повертаємо ефект у його бік, щоб іскри летіли природно
                Quaternion rotation = attacker != null ? Quaternion.LookRotation(attacker.transform.forward) : Quaternion.identity;

                // Спавнимо ефект по центру об'єкта (якщо є коллайдер)
                Vector3 spawnPos = GetComponent<Collider>() != null ? GetComponent<Collider>().bounds.center : transform.position + Vector3.up;

                GameObject vfx = Instantiate(_data.HitVFXPrefab, spawnPos, rotation);
                Destroy(vfx, 2f); // Видаляємо через 2 сек, щоб не засмічувати сцену
            }
        }
        // ==========================================

        private float CalculateHarvestDamage(DamageRequest request)
        {
            float damage = 0f;
            var profile = request.DamageProfile;
            if (profile != null && profile.Count > 0)
            {
                for (int i = 0; i < profile.Count; i++)
                {
                    var instance = profile[i];
                    if (instance.Amount <= 0f) continue;
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

        private static string WeaponName(DamageRequest request) { return request.Weapon != null ? request.Weapon.ItemName : "None"; }
        private static string WeaponToolName(DamageRequest request) { return request.Weapon != null ? request.Weapon.HarvestTool.ToString() : HarvestToolType.None.ToString(); }

        private bool IsMatchingTool(DamageRequest request)
        {
            if (_data == null || _data.PreferredTool == HarvestToolType.None) return true;
            HarvestToolType usedTool = request.Weapon != null ? request.Weapon.HarvestTool : HarvestToolType.None;
            return usedTool == _data.PreferredTool;
        }

        private float YieldMultiplier(bool matchingTool)
        {
            if (_data == null || matchingTool) return 1f;
            return Mathf.Clamp01(_data.MismatchedYieldMultiplier);
        }

        private void GrantBaseYield(float healthRemoved, GameObject source, float yieldMultiplier)
        {
            if (_data.BaseYieldItem == null || _data.HealthPerBaseYield <= 0f || healthRemoved <= 0f) return;

            _baseYieldHealth += healthRemoved;
            int thresholds = Mathf.FloorToInt(_baseYieldHealth / _data.HealthPerBaseYield);
            if (thresholds <= 0) return;

            _baseYieldHealth -= thresholds * _data.HealthPerBaseYield;
            int amount = thresholds * Mathf.Max(1, _data.BaseYieldAmount);
            GrantItem(_data.BaseYieldItem, amount, source, yieldMultiplier);
        }

        private void CompleteHarvest(GameObject source, float yieldMultiplier)
        {
            _depleted = true;
            SetHovered(false);

            // ==========================================
            // ДОДАНО: Звук повного знищення (наприклад, дерево падає або камінь тріскається)
            // ==========================================
            if (_data.DestructionSound != null)
            {
                AudioSource.PlayClipAtPoint(_data.DestructionSound, transform.position, 1f);
            }
            // ==========================================

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

            var depletionHandler = GetComponent<IResourceDepletionHandler>();
            if (depletionHandler != null)
                depletionHandler.HandleResourceDepleted(this);
            else
                DestroyNode();
        }

        private static SoulAshWallet ResolveWallet(GameObject source, InventoryController inventory)
        {
            if (source != null)
            {
                var wallet = source.GetComponentInParent<SoulAshWallet>();
                if (wallet != null) return wallet;
            }
            return inventory != null ? inventory.GetComponent<SoulAshWallet>() : null;
        }

        private void GrantDrop(ResourceDrop drop, GameObject source, InventoryController inventory, float yieldMultiplier)
        {
            if (drop == null || drop.Item == null || drop.Chance <= 0f) return;
            if (drop.Chance < 1f && UnityEngine.Random.value > drop.Chance) return;

            int amount = drop.RollAmount();
            if (amount <= 0) return;

            GrantItem(drop.Item, amount, source, yieldMultiplier, inventory);
        }

        private void GrantItem(ItemData item, int amount, GameObject source, float yieldMultiplier, InventoryController inventory = null)
        {
            if (item == null || amount <= 0) return;
            if (inventory == null && source != null)
                inventory = source.GetComponentInParent<InventoryController>();

            int finalAmount = ScaleYieldAmount(item, amount, yieldMultiplier);
            if (finalAmount <= 0) return;

            WorldItemDropUtility.GrantOrDrop(item, finalAmount, inventory, transform.position, "[Harvesting]");
        }

        private int ScaleYieldAmount(ItemData item, int amount, float yieldMultiplier)
        {
            if (amount <= 0 || yieldMultiplier <= 0f) return 0;
            if (Mathf.Approximately(yieldMultiplier, 1f)) return amount;

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

        private string NodeName() { return _data != null ? _data.NodeName : name; }

        private void DestroyNode()
        {
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        private void EnsurePresentation()
        {
            if (_createFallbackPresentation && GetComponent<Collider>() == null)
                gameObject.AddComponent<BoxCollider>();

            _feedbackRenderers = GetComponentsInChildren<Renderer>();
            if ((_feedbackRenderers == null || _feedbackRenderers.Length == 0) && _createFallbackPresentation)
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
                    if (Application.isPlaying) Destroy(visualCollider);
                    else DestroyImmediate(visualCollider);
                }

                _feedbackRenderers = new[] { visual.GetComponent<Renderer>() };
            }
        }

        private void ShowHitFeedback()
        {
            if (_feedbackRenderers == null || _feedbackRenderers.Length == 0) return;
            if (_feedbackBlock == null) _feedbackBlock = new MaterialPropertyBlock();

            foreach (var renderer in _feedbackRenderers)
            {
                if (renderer == null) continue;
                int materialCount = renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 0;
                for (int i = 0; i < materialCount; i++)
                {
                    renderer.GetPropertyBlock(_feedbackBlock, i);
                    ApplyFeedbackColor(_feedbackBlock);
                    renderer.SetPropertyBlock(_feedbackBlock, i);
                }
            }

            _feedbackActive = true;
            _feedbackUntil = Time.time + 0.12f;
            enabled = true;
        }

        private void ClearHitFeedback()
        {
            if (_feedbackRenderers == null) return;
            foreach (var renderer in _feedbackRenderers)
            {
                if (renderer == null) continue;
                int materialCount = renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 0;
                for (int i = 0; i < materialCount; i++)
                    renderer.SetPropertyBlock(null, i);
            }

            _feedbackActive = false;
            enabled = false;
        }

        private void ApplyFeedbackColor(MaterialPropertyBlock block)
        {
            block.SetColor(BaseColorId, _hitFeedbackColor);
            block.SetColor(ColorId, _hitFeedbackColor);
            block.SetColor(TintColorId, _hitFeedbackColor);
            block.SetColor(HueVariationId, _hitFeedbackColor);
        }

        private void MarkPresentationDepleted()
        {
            var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }
    }
}