// Підключаємо правильний простір імен вашого інвентарю
using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class HotbarUI : MonoBehaviour
    {
        [SerializeField] private EquipmentSlots _equipment;
        [SerializeField] private Image[] _slotIcons = new Image[3];
        [SerializeField] private Image[] _selectionBorders = new Image[3];

        private void Update()
        {
            if (_equipment == null) return;

            for (int i = 0; i < _slotIcons.Length; i++)
            {
                ItemData item = _equipment.GetItemInSlot(i);

                if (item != null && item.Icon != null)
                {
                    _slotIcons[i].sprite = item.Icon;
                    if (!_slotIcons[i].gameObject.activeSelf) _slotIcons[i].gameObject.SetActive(true);
                }
                else
                {
                    if (_slotIcons[i].gameObject.activeSelf) _slotIcons[i].gameObject.SetActive(false);
                }

                if (i < _selectionBorders.Length && _selectionBorders[i] != null)
                {
                    _selectionBorders[i].enabled = (i == _equipment.CurrentSlotIndex);
                }
            }
        }
    }
}