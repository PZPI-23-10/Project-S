using Project_S.Runtime.Gameplay.Character.Inventory; // Підключаємо доступ до ItemData
using TMPro;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class TooltipUI : MonoBehaviour
    {
        public static TooltipUI Instance { get; private set; }

        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _weightText;
        [SerializeField] private TMP_Text _descriptionText;

        private void Awake()
        {
            Instance = this;
            Hide();
        }

        private void Update()
        {
            // Щокадру рухаємо панель за мишкою з відступом, 
            // щоб курсор не перекривав текст і не заважав клікати.
            transform.position = UnityEngine.Input.mousePosition + new Vector3(15f, -15f, 0f);
        }

        public void Show(ItemData item)
        {
            gameObject.SetActive(true);
            if (_titleText != null) _titleText.text = item.ItemName;
            if (_weightText != null) _weightText.text = $"Вага: {item.Weight} кг";
            if (_descriptionText != null) _descriptionText.text = item.Description;

            // Миттєво ставимо в позицію миші при появі
            transform.position = UnityEngine.Input.mousePosition + new Vector3(15f, -15f, 0f);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}