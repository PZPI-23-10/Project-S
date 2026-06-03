using System;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Input;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Inventory
{
    public class EquipmentSlots : MonoBehaviour
    {
        [Header("Слоти (Хотбар)")]
        [SerializeField] private ItemData[] _slots = new ItemData[3];

        [Header("Зв'язки з візуалом")]
        [SerializeField] private Transform _weaponAnchor;
        [SerializeField] private GameObject _fistsObject;
        [SerializeField] private PoiseController _poise;
        [SerializeField] private PlayerActionGate _actionGate;

        private GameObject _spawnedWeapon;
        private int _currentSlot = 0;

        public int CurrentSlotIndex => _currentSlot;
        public event Action Changed;

        public float TotalWeight
        {
            get
            {
                float total = 0;
                foreach (var item in _slots)
                {
                    if (item != null) total += item.Weight;
                }
                return total;
            }
        }

        private void Start()
        {
            if (_actionGate == null) _actionGate = GetComponentInParent<PlayerActionGate>();
            SwitchToSlot(0);
        }

        public void Tick(PlayerInputSnapshot input)
        {
            if (_poise != null && _poise.IsBroken) return;

            if (input.HotbarSlotPressed >= 0 && input.HotbarSlotPressed < _slots.Length)
                SwitchToSlot(input.HotbarSlotPressed);
        }

        public void SwitchToSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length) return;

            _currentSlot = slotIndex;
            ItemData item = _slots[slotIndex];

            if (_spawnedWeapon != null)
            {
                if (Application.isPlaying)
                    Destroy(_spawnedWeapon);
                else
                    DestroyImmediate(_spawnedWeapon);

                _spawnedWeapon = null;
            }

            if (item != null && item.WeaponPrefab != null)
            {
                if (_fistsObject != null) _fistsObject.SetActive(false);

                _spawnedWeapon = Instantiate(item.WeaponPrefab, _weaponAnchor);
                _spawnedWeapon.transform.localPosition = Vector3.zero;
                _spawnedWeapon.transform.localRotation = Quaternion.identity;
            }
            else
            {
                if (_fistsObject != null) _fistsObject.SetActive(true);
            }

            Changed?.Invoke();
        }

        public ItemData GetItemInSlot(int index)
        {
            if (index < 0 || index >= _slots.Length) return null;
            return _slots[index];
        }

        public void EquipItem(ItemData newItem)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = newItem;
                    if (_currentSlot == i) SwitchToSlot(i);
                    else Changed?.Invoke();
                    return;
                }
            }

            _slots[_currentSlot] = newItem;
            SwitchToSlot(_currentSlot);
        }

        public int GetSize()
        {
            return _slots != null ? _slots.Length : 0;
        }

        public void RestoreSlots(IReadOnlyList<ItemData> items, int currentSlot)
        {
            if (_slots == null || _slots.Length == 0)
                _slots = new ItemData[3];

            for (int i = 0; i < _slots.Length; i++)
                _slots[i] = items != null && i < items.Count ? items[i] : null;

            SwitchToSlot(Mathf.Clamp(currentSlot, 0, _slots.Length - 1));
            Changed?.Invoke();
        }
    }
}