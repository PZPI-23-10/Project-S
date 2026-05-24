using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Inventory
{
    public enum AccessorySlotType
    {
        Ring,
        Amulet,
        Charm
    }

    [CreateAssetMenu(fileName = "NewAccessory", menuName = "Project-S/Items/Accessory")]
    public class AccessoryItemData : ItemData
    {
        public AccessorySlotType SlotType = AccessorySlotType.Ring;
        public List<StatModifier> StatModifiers = new List<StatModifier>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            Kind = ItemKind.Accessory;
            IsStackable = false;
            MaxStack = 1;
        }
#endif
    }
}
