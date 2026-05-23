using UnityEngine.UI;
using Project_S.Runtime.Core.Services;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;
using Zenject;
using TMPro; // Додали для роботи з текстом

namespace Project_S.Runtime.Gameplay.HUD
{
    public class SurvivalStatSlider : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private RectTransform _sliderRect;
        [SerializeField] private TMP_Text _valueText; // Сюди перетягнеш свої цифри (100)

        [Tooltip("Додай Canvas Group на стаміну і перетягни сюди, щоб вона ховалася")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Settings")]
        [SerializeField] private StatType _statType;
        [SerializeField] private bool _invert;

        [Tooltip("Постав галочку для Стаміни, щоб вона зникала при 100%")]
        [SerializeField] private bool _hideWhenFull;

        [Inject] private PlayerProvider _playerProvider;

        private CharacterStats _currentStats;
        private float _originalWidth;

        private void Awake()
        {
            if (_sliderRect != null)
            {
                _originalWidth = _sliderRect.rect.width;
            }
        }

        private void Start()
        {
            OnPlayerChanged();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void OnPlayerChanged()
        {
            Unsubscribe();

            _currentStats = _playerProvider.Player.Stats;
            _currentStats.Changed += OnStatChanged;

            Refresh();
        }

        private void OnStatChanged(StatType statType, float value)
        {
            if (statType != _statType)
                return;

            Refresh();
        }

        private void Refresh()
        {
            if (_sliderRect == null || _currentStats == null) return;

            var value = _currentStats.GetNormalized(_statType);

            if (_invert)
                value = 1f - value;

            // 1. Рухаємо смужку
            _sliderRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _originalWidth * value);

            // 2. Оновлюємо цифри (множимо на 100, щоб були гарні цілі числа 0-100)
            if (_valueText != null)
            {
                _valueText.text = Mathf.RoundToInt(value * 100f).ToString();
            }

            // 3. Магія ARC Raiders: ховаємо стаміну, якщо вона повна
            if (_hideWhenFull && _canvasGroup != null)
            {
                if (value >= 0.99f)
                    _canvasGroup.alpha = 0f; // Робимо повністю прозорою
                else
                    _canvasGroup.alpha = 1f; // Показуємо
            }
        }

        private void Unsubscribe()
        {
            if (_currentStats == null)
                return;

            _currentStats.Changed -= OnStatChanged;
            _currentStats = null;
        }
    }
}