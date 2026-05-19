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

        private void Update()
        {
            if (_actionGate != null && _actionGate.IsGameplayBlocked) return;
            if (_poise != null && _poise.IsBroken) return;

            // Виправлено: явно вказуємо UnityEngine.Input, щоб уникнути конфлікту імен
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) SwitchToSlot(0);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) SwitchToSlot(1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) SwitchToSlot(2);
        }

        public void SwitchToSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length) return;

            _currentSlot = slotIndex;
            ItemData item = _slots[slotIndex];

            if (_spawnedWeapon != null) Destroy(_spawnedWeapon);

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
                    return;
                }
            }

            _slots[_currentSlot] = newItem;
            SwitchToSlot(_currentSlot);
        }
    }
}