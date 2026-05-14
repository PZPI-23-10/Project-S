using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Inventory
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Project-S/Inventory/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Основна інформація")]
        public string ItemName = "Предмет";
        public float Weight = 1.0f;

        [Header("Інтерфейс (UI)")]
        public Sprite Icon;
        [TextArea(3, 5)] public string Description = "Опис предмета для підказки...";

        [Header("Бойові параметри")]
        public GameObject WeaponPrefab;
        public float Damage = 20f;
    }
}