using Project_S.Runtime.Core.Services;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class SurvivalStatSlider : MonoBehaviour
    {
        [SerializeField] private Image _imageSlider;
        [SerializeField] private StatType _statType;
        [SerializeField] private bool _invert;

        [Inject] private PlayerProvider _playerProvider;

        private CharacterStats _currentStats;

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
            var value = _currentStats.GetNormalized(_statType);

            if (_invert)
                value = 1f - value;

            _imageSlider.fillAmount = value;
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
