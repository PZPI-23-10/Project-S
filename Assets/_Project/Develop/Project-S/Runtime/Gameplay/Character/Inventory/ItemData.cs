using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Inventory
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Project-S/Inventory/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Основна інформація")]
        public string ItemName = "Предмет";
        public float Weight = 1.0f;

        [Header("Стакування (Stacks)")]
        public bool IsStackable = false; 
        public int MaxStack = 1;         

        [Header("Інтерфейс (UI)")]
        public Sprite Icon;
        [TextArea(3, 5)] public string Description = "Опис...";

        [Header("Префаби")]
        public GameObject WeaponPrefab;      
        public GameObject WorldPickupPrefab; 
    }
}