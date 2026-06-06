using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class BossHealthBarUI : MonoBehaviour
    {
        public static BossHealthBarUI Instance { get; private set; }

        [Header("UI Components")]
        [SerializeField] private GameObject _container;
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private Image _fillImage;
        [SerializeField] private TMP_Text _bossNameText;
        [SerializeField] private RectTransform _barRectTransform;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (_container != null)
                _container.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Show(string bossName, float normalizedHealth, Color fillColor, float width, float height, Vector3 scale)
        {
            if (_container != null)
                _container.SetActive(true);

            if (_bossNameText != null)
                _bossNameText.text = bossName;

            if (_fillImage != null)
            {
                _fillImage.color = fillColor;
                _fillImage.rectTransform.anchorMin = Vector2.zero;
                _fillImage.rectTransform.anchorMax = Vector2.one;
                _fillImage.rectTransform.offsetMin = Vector2.zero;
                _fillImage.rectTransform.offsetMax = Vector2.zero;
            }

            if (_barRectTransform != null)
            {
                _barRectTransform.sizeDelta = new Vector2(width, height);
                _barRectTransform.localScale = scale;
            }

            UpdateHealth(normalizedHealth);
        }

        public void UpdateHealth(float normalizedHealth)
        {
            if (_healthSlider != null)
                _healthSlider.value = Mathf.Clamp01(normalizedHealth);
            else if (_fillImage != null && _fillImage.type == Image.Type.Filled)
                _fillImage.fillAmount = Mathf.Clamp01(normalizedHealth);
        }

        public void Hide()
        {
            if (_container != null)
                _container.SetActive(false);
        }
    }
}
