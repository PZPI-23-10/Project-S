using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Crafting;
using Project_S.Runtime.Gameplay.Loot;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public class SimpleEnemy : MonoBehaviour, IDamageReceiver
    {
        [SerializeField] private float _health = 100f;
        [SerializeField] private int _soulAshReward = 10;
        [SerializeField] private LootDropper _lootDropper;

        private bool _dead;

        public float Health
        {
            get => _health;
            set => _health = value;
        }

        public int SoulAshReward
        {
            get => _soulAshReward;
            set => _soulAshReward = Mathf.Max(0, value);
        }

        private void Awake()
        {
            if (_lootDropper == null)
                _lootDropper = GetComponent<LootDropper>();
        }

        public void ReceiveDamage(DamageRequest request)
        {
            if (_dead)
                return;

            _health -= request.HealthDamage;
            Debug.Log($"[Enemy] {name} took {request.HealthDamage:F1} damage. HP: {_health:F1}");

            if (_health <= 0f)
                Die(request.Source);
        }

        private void Die(GameObject source)
        {
            _dead = true;

            if (_lootDropper == null)
                _lootDropper = GetComponent<LootDropper>();

            if (_lootDropper != null)
                _lootDropper.DropFor(source, _soulAshReward);
            else
                TryGrantSoulAsh(source);

            if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
        }

        private void TryGrantSoulAsh(GameObject source)
        {
            if (_soulAshReward <= 0 || source == null)
                return;

            var wallet = source.GetComponentInParent<SoulAshWallet>();
            if (wallet == null)
            {
                var inventory = source.GetComponentInParent<InventoryController>();
                if (inventory != null)
                    wallet = inventory.GetComponent<SoulAshWallet>();
            }

            if (wallet != null)
                wallet.AddReward(_soulAshReward, source);
        }
    }
}
