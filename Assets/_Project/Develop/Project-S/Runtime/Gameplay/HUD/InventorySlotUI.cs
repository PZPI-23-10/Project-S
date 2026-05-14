using Project_S.Runtime.Gameplay.Character.Inventory; // Підключаємо доступ до ItemData та EquipmentSlots
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Project_S.Runtime.Gameplay.HUD
{
    // Додали IPointerClickHandler для обробки кліку
    public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image _iconImage;

        private ItemData _currentItem;
        private EquipmentSlots _equipment;

        public void Setup(ItemData item)
        {
            _currentItem = item;

            // Шукаємо EquipmentSlots на Гравцеві (один раз для оптимізації)
            if (_equipment == null)
            {
                _equipment = FindFirstObjectByType<EquipmentSlots>();
            }

            if (item != null && item.Icon != null)
            {
                _iconImage.sprite = item.Icon;
                _iconImage.gameObject.SetActive(true);
            }
            else
            {
                _iconImage.gameObject.SetActive(false);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_currentItem != null && TooltipUI.Instance != null)
            {
                TooltipUI.Instance.Show(_currentItem);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (TooltipUI.Instance != null)
            {
                TooltipUI.Instance.Hide();
            }
        }

        // --- ОБРОБКА КЛІКУ (Екіпірування в Хотбар) ---
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && _currentItem != null)
            {
                if (_equipment != null)
                {
                    _equipment.EquipItem(_currentItem);
                    Debug.Log($"<color=cyan>[UI]</color> Клік по слоту: {_currentItem.ItemName} відправлено в Хотбар!");
                }
            }
        }
    }
}