using System;
using Project_S.Runtime.Gameplay.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class UpgradeNodeView : MonoBehaviour
    {
        [SerializeField] private UpgradeDefinition _definition;
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _label;

        [Header("Colors")]
        [SerializeField] private Color _lockedColor = new Color(0.11f, 0.12f, 0.14f, 0.86f);
        [SerializeField] private Color _availableColor = new Color(0.22f, 0.25f, 0.29f, 0.95f);
        [SerializeField] private Color _purchasedColor = new Color(0.23f, 0.56f, 0.35f, 0.95f);
        [SerializeField] private Color _selectedColor = new Color(0.96f, 0.72f, 0.24f, 1f);

        public UpgradeDefinition Definition => _definition;

        private void Reset()
        {
            ResolveReferences();
        }

        public void SetClickHandler(Action<UpgradeNodeView> handler)
        {
            ResolveReferences();

            if (_button == null)
                return;

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => handler?.Invoke(this));
        }

        public void Refresh(PlayerUpgradeController controller, bool selected)
        {
            ResolveReferences();

            if (_button != null)
                _button.interactable = true;

            if (_label != null && _definition != null)
                _label.text = _definition.Id;

            if (_icon != null && _definition != null)
            {
                _icon.sprite = _definition.Icon;
                _icon.enabled = _definition.Icon != null;
            }

            if (_background == null || controller == null || _definition == null)
                return;

            if (selected)
                _background.color = _selectedColor;
            else if (controller.HasUpgrade(_definition.Id))
                _background.color = _purchasedColor;
            else
                _background.color = controller.Check(_definition).CanPurchase ? _availableColor : _lockedColor;
        }

        private void ResolveReferences()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_background == null)
                _background = GetComponent<Image>();

            if (_label == null)
                _label = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
