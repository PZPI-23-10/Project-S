using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Crafting;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Harvesting
{
    public class HarvestableResourceNode : MonoBehaviour, IDamageReceiver
    {
        [SerializeField] private ResourceNodeData _data;
        [SerializeField] private float _currentHealth;

        private bool _depleted;

        public ResourceNodeData Data => _data;
        public float CurrentHealth => _currentHealth;
        public bool IsDepleted => _depleted;

        public void Configure(ResourceNodeData data)
        {
            _data = data;
            ResetHealth();
        }

        private void Awake()
        {
            ResetHealth();
        }

        public void ReceiveDamage(DamageRequest request)
        {
            if (_depleted || _data == null)
                return;

            float damage = CalculateHarvestDamage(request);
            if (damage <= 0f)
                return;

            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            Debug.Log($"[Harvesting] {NodeName()} took {damage:F1} harvest damage. HP: {_currentHealth:F1}/{_data.MaxHealth:F1}");

            if (_currentHealth <= 0f)
                CompleteHarvest(request.Source);
        }

        private float CalculateHarvestDamage(DamageRequest request)
        {
            float baseDamage = Mathf.Max(0f, request.HealthDamage);
            if (_data.PreferredTool == HarvestToolType.None)
                return baseDamage * _data.MatchingToolDamageMultiplier;

            HarvestToolType usedTool = request.Weapon != null ? request.Weapon.HarvestTool : HarvestToolType.None;
            bool matchingTool = usedTool == _data.PreferredTool;
            float multiplier = matchingTool
                ? _data.MatchingToolDamageMultiplier
                : _data.MismatchedToolDamageMultiplier;

            return baseDamage * multiplier;
        }

        private void CompleteHarvest(GameObject source)
        {
            _depleted = true;

            InventoryController inventory = source != null ? source.GetComponentInParent<InventoryController>() : null;
            SoulAshWallet wallet = ResolveWallet(source, inventory);

            if (wallet != null && _data.SoulAshReward > 0)
                wallet.AddReward(_data.SoulAshReward, source);

            if (_data.Drops != null)
            {
                foreach (var drop in _data.Drops)
                    GrantDrop(drop, inventory);
            }

            Debug.Log($"[Harvesting] {NodeName()} harvested.");
            DestroyNode();
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

        private void GrantDrop(ResourceDrop drop, InventoryController inventory)
        {
            if (drop == null || drop.Item == null || drop.Chance <= 0f)
                return;

            if (drop.Chance < 1f && UnityEngine.Random.value > drop.Chance)
                return;

            int amount = drop.RollAmount();
            if (amount <= 0)
                return;

            WorldItemDropUtility.GrantOrDrop(drop.Item, amount, inventory, transform.position, "[Harvesting]");
        }

        private void ResetHealth()
        {
            _currentHealth = _data != null ? Mathf.Max(1f, _data.MaxHealth) : 1f;
            _depleted = false;
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
    }
}
