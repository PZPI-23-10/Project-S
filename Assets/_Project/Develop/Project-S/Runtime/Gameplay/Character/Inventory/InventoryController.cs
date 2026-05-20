using System;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Inventory
{
    [Serializable]
    public class ItemStack
    {
        public ItemData Item;
        public int Amount;

        public ItemStack() { }

        public ItemStack(ItemData item, int amount)
        {
            Item = item;
            Amount = amount;
        }
    }

    public class InventoryController : MonoBehaviour
    {
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private int _inventorySize = 16;

        private ItemStack[] _slots;
        private BuffController _buffs;

        public Action OnInventoryChanged;

        private void Awake()
        {
            if (_stats == null) _stats = GetComponent<CharacterStats>();
            _buffs = GetComponent<BuffController>();
            EnsureSlots();
        }

        public float GetMaxWeight() => _stats != null ? _stats.Get(StatType.CarryWeight) : 50f;

        public float GetCurrentWeight()
        {
            EnsureSlots();

            float total = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null && _slots[i].Item != null)
                    total += _slots[i].Item.Weight * Mathf.Max(0, _slots[i].Amount);
            }

            return total;
        }

        public float GetWeightPenaltyMultiplier()
        {
            float current = GetCurrentWeight();
            float max = GetMaxWeight();

            if (max <= 0) return 1f;

            float ratio = current / max;

            if (ratio >= 1.5f) return 0f;
            if (ratio >= 1.2f) return 0.4f;
            if (ratio >= 1.0f) return 0.95f;

            return 1f;
        }

        public bool AddItem(ItemData item, int amountToAdd = 1)
        {
            EnsureSlots();

            if (!CanAddItem(item, amountToAdd))
            {
                Debug.LogWarning("[Inventory] Not enough inventory space.");
                return false;
            }

            AddItemUnchecked(item, amountToAdd);
            NotifyInventoryChanged();
            return true;
        }

        public bool CanAddItem(ItemData item, int amountToAdd = 1)
        {
            EnsureSlots();

            if (item == null || amountToAdd <= 0) return false;

            var simulatedSlots = CreateSlotSnapshot();
            return CanAddToSnapshot(simulatedSlots, item, amountToAdd);
        }

        public bool CanAddItemAfterRemoving(ItemData item, int amountToAdd, IReadOnlyList<ItemStack> removals)
        {
            EnsureSlots();

            if (item == null || amountToAdd <= 0) return false;

            var simulatedSlots = CreateSlotSnapshot();
            if (removals != null)
            {
                foreach (var removal in removals)
                {
                    if (!RemoveFromSnapshot(simulatedSlots, removal.Item, removal.Amount))
                        return false;
                }
            }

            return CanAddToSnapshot(simulatedSlots, item, amountToAdd);
        }

        public int GetItemCount(ItemData item)
        {
            EnsureSlots();

            if (item == null) return 0;

            int count = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null && _slots[i].Item == item)
                    count += Mathf.Max(0, _slots[i].Amount);
            }

            return count;
        }

        public bool CanRemoveItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return false;
            return GetItemCount(item) >= amount;
        }

        public bool TryRemoveItem(ItemData item, int amount)
        {
            EnsureSlots();

            if (!CanRemoveItem(item, amount)) return false;

            int remaining = amount;
            for (int i = _slots.Length - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = _slots[i];
                if (slot == null || slot.Item != item) continue;

                int take = Mathf.Min(slot.Amount, remaining);
                slot.Amount -= take;
                remaining -= take;

                if (slot.Amount <= 0)
                    _slots[i] = null;
            }

            NotifyInventoryChanged();
            return true;
        }

        public bool TryConsumeSlot(int index, int amount = 1)
        {
            EnsureSlots();

            if (index < 0 || index >= _slots.Length || amount <= 0) return false;

            var slot = _slots[index];
            if (slot == null || slot.Item == null || slot.Amount < amount) return false;

            slot.Amount -= amount;
            if (slot.Amount <= 0)
                _slots[index] = null;

            NotifyInventoryChanged();
            return true;
        }

        public bool TryUseItemAtSlot(int index)
        {
            EnsureSlots();

            if (index < 0 || index >= _slots.Length) return false;

            var slot = _slots[index];
            if (slot == null || slot.Item == null) return false;

            if (!ApplyConsumableEffect(slot.Item)) return false;

            return TryConsumeSlot(index);
        }

        public ItemStack GetSlot(int index)
        {
            EnsureSlots();

            if (index < 0 || index >= _slots.Length) return null;
            return _slots[index];
        }

        public void SetSlot(int index, ItemStack stack)
        {
            EnsureSlots();

            if (index < 0 || index >= _slots.Length) return;
            _slots[index] = stack;
            NormalizeSlot(index);
            NotifyInventoryChanged();
        }

        public ItemStack[] GetAllSlots()
        {
            EnsureSlots();
            return _slots;
        }

        public int GetSize() => _inventorySize;

        private bool ApplyConsumableEffect(ItemData item)
        {
            if (item == null) return false;

            bool applied = false;

            if (_stats != null)
            {
                if (item.StatEffects != null)
                {
                    foreach (var statEffect in item.StatEffects)
                    {
                        if (statEffect == null || Mathf.Approximately(statEffect.Amount, 0f))
                            continue;

                        _stats.Add(statEffect.StatType, statEffect.Amount);
                        applied = true;
                    }
                }

                if (!Mathf.Approximately(item.HealthRestoreAmount, 0f))
                {
                    _stats.Add(StatType.Health, item.HealthRestoreAmount);
                    applied = true;
                }

                if (!Mathf.Approximately(item.HungerRestoreAmount, 0f))
                {
                    _stats.Add(StatType.Hunger, -item.HungerRestoreAmount);
                    applied = true;
                }

                if (!Mathf.Approximately(item.StaminaRestoreAmount, 0f))
                {
                    _stats.Add(StatType.Stamina, item.StaminaRestoreAmount);
                    applied = true;
                }
            }

            if (ApplyTimedBuff(item.TimedBuffType, item.TimedBuffCategory, item.TimedBuffMultiplier, item.TimedBuffDurationSeconds))
                applied = true;

            if (ApplyTimedBuff(item.SecondaryTimedBuffType, item.SecondaryTimedBuffCategory, item.SecondaryTimedBuffMultiplier, item.SecondaryTimedBuffDurationSeconds))
                applied = true;

            if (item.TimedBuffs != null)
            {
                foreach (var timedBuff in item.TimedBuffs)
                {
                    if (timedBuff == null)
                        continue;

                    if (ApplyTimedBuff(timedBuff.Type, timedBuff.Category, timedBuff.Multiplier, timedBuff.DurationSeconds))
                        applied = true;
                }
            }

            if (item.DamageConversions != null)
            {
                foreach (var conversion in item.DamageConversions)
                {
                    if (ApplyDamageConversion(conversion))
                        applied = true;
                }
            }

            if (ApplySpecialEffect(item.SpecialEffect, item.SpecialEffectDelaySeconds))
                applied = true;

            if (item.SpecialEffects != null)
            {
                foreach (var specialEffect in item.SpecialEffects)
                {
                    if (specialEffect == null)
                        continue;

                    if (ApplySpecialEffect(specialEffect.Type, specialEffect.DelaySeconds))
                        applied = true;
                }
            }

            return applied;
        }

        private bool ApplyTimedBuff(TimedBuffType type, TimedBuffCategory category, float multiplier, float durationSeconds)
        {
            if (type == TimedBuffType.None || durationSeconds <= 0f)
                return false;

            if (_buffs == null)
                _buffs = GetComponent<BuffController>() ?? gameObject.AddComponent<BuffController>();

            _buffs.ApplyBuff(type, category, multiplier, durationSeconds);
            return true;
        }

        private bool ApplyDamageConversion(DamageConversionEffect conversion)
        {
            if (conversion == null || !conversion.IsValid())
                return false;

            if (_buffs == null)
                _buffs = GetComponent<BuffController>() ?? gameObject.AddComponent<BuffController>();

            _buffs.ApplyDamageConversion(conversion);
            return true;
        }

        private bool ApplySpecialEffect(ConsumableSpecialEffectType type, float delaySeconds)
        {
            if (type == ConsumableSpecialEffectType.None)
                return false;

            if (type == ConsumableSpecialEffectType.HomeTeleport)
            {
                var teleport = GetComponent<HomeTeleportController>() ?? gameObject.AddComponent<HomeTeleportController>();
                teleport.StartTeleport(delaySeconds > 0f ? delaySeconds : 5f);
                return true;
            }

            return false;
        }

        private void AddItemUnchecked(ItemData item, int amountToAdd)
        {
            int remaining = amountToAdd;
            int maxStack = GetMaxStack(item);

            if (item.IsStackable)
            {
                for (int i = 0; i < _slots.Length && remaining > 0; i++)
                {
                    var slot = _slots[i];
                    if (slot == null || slot.Item != item || slot.Amount >= maxStack) continue;

                    int add = Mathf.Min(maxStack - slot.Amount, remaining);
                    slot.Amount += add;
                    remaining -= add;
                }
            }

            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                if (_slots[i] != null && _slots[i].Item != null) continue;
                if (!CanCreateNewStack(CreateSlotSnapshot(), item)) break;

                int add = item.IsStackable ? Mathf.Min(maxStack, remaining) : 1;
                _slots[i] = new ItemStack(item, add);
                remaining -= add;
            }
        }

        private static bool CanAddToSnapshot(List<ItemStack> slots, ItemData item, int amountToAdd)
        {
            int remaining = amountToAdd;
            int maxStack = GetMaxStack(item);

            if (item.IsStackable)
            {
                foreach (var slot in slots)
                {
                    if (slot == null || slot.Item != item || slot.Amount >= maxStack) continue;

                    int add = Mathf.Min(maxStack - slot.Amount, remaining);
                    slot.Amount += add;
                    remaining -= add;
                    if (remaining <= 0) return true;
                }
            }

            foreach (var slot in slots)
            {
                if (slot != null && slot.Item != null) continue;

                if (!CanCreateNewStack(slots, item))
                    return false;

                int add = item.IsStackable ? Mathf.Min(maxStack, remaining) : 1;
                slot.Item = item;
                slot.Amount = add;
                remaining -= add;
                if (remaining <= 0) return true;
            }

            return remaining <= 0;
        }

        private static bool CanCreateNewStack(List<ItemStack> slots, ItemData item)
        {
            if (item == null || item.MaxInventoryStacks <= 0)
                return true;

            int stacks = 0;
            foreach (var slot in slots)
            {
                if (slot != null && slot.Item == item && slot.Amount > 0)
                    stacks++;
            }

            return stacks < item.MaxInventoryStacks;
        }

        private static bool RemoveFromSnapshot(List<ItemStack> slots, ItemData item, int amount)
        {
            if (item == null || amount <= 0) return false;

            int remaining = amount;
            for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = slots[i];
                if (slot == null || slot.Item != item) continue;

                int take = Mathf.Min(slot.Amount, remaining);
                slot.Amount -= take;
                remaining -= take;

                if (slot.Amount <= 0)
                {
                    slot.Item = null;
                    slot.Amount = 0;
                }
            }

            return remaining <= 0;
        }

        private List<ItemStack> CreateSlotSnapshot()
        {
            var snapshot = new List<ItemStack>(_slots.Length);
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                snapshot.Add(slot == null ? new ItemStack(null, 0) : new ItemStack(slot.Item, slot.Amount));
            }

            return snapshot;
        }

        private void EnsureSlots()
        {
            if (_inventorySize <= 0)
                _inventorySize = 1;

            if (_slots != null && _slots.Length == _inventorySize)
                return;

            var oldSlots = _slots;
            _slots = new ItemStack[_inventorySize];

            if (oldSlots == null) return;

            int count = Mathf.Min(oldSlots.Length, _slots.Length);
            for (int i = 0; i < count; i++)
                _slots[i] = oldSlots[i];
        }

        private void NormalizeSlot(int index)
        {
            var slot = _slots[index];
            if (slot == null || slot.Item == null || slot.Amount <= 0)
            {
                _slots[index] = null;
                return;
            }

            slot.Amount = Mathf.Min(slot.Amount, GetMaxStack(slot.Item));
        }

        private static int GetMaxStack(ItemData item)
        {
            if (item == null) return 1;
            return item.IsStackable ? Mathf.Max(1, item.MaxStack) : 1;
        }

        private void NotifyInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
        }
    }
}
