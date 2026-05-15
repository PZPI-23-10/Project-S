using Project_S.Runtime.Gameplay.Character.Inventory;
using System.Collections; // ƒќƒјЌќ ƒЋя “ј…ћ≈–ј
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _amountText;
        [SerializeField] private float _tooltipDelay = 0.5f; // „ј— «ј“–»ћ » (п≥в секунди)

        private int _slotIndex;
        private InventoryUI _uiManager;
        private ItemStack _cachedStack;

        private Coroutine _tooltipCoroutine; // «м≥нна, €ка пам'€таЇ наш запущений таймер

        public void Init(int slotIndex, InventoryUI uiManager)
        {
            _slotIndex = slotIndex;
            _uiManager = uiManager;
        }

        public void UpdateView(ItemStack stack)
        {
            _cachedStack = stack;

            if (stack != null && stack.Item != null && stack.Item.Icon != null)
            {
                _iconImage.sprite = stack.Item.Icon;
                _iconImage.gameObject.SetActive(true);

                if (_amountText != null)
                {
                    _amountText.text = stack.Amount > 1 ? stack.Amount.ToString() : "";
                    _amountText.gameObject.SetActive(stack.Amount > 1);
                }
            }
            else
            {
                _iconImage.gameObject.SetActive(false);
                if (_amountText != null) _amountText.gameObject.SetActive(false);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // якщо ми кл≥кнули (вз€ли предмет), одразу ховаЇмо п≥дказку ≥ зупин€Їмо таймер
            StopTooltipTimer();

            if (_uiManager != null)
            {
                _uiManager.OnSlotClicked(_slotIndex, eventData.button);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // якщо в слот≥ Ї предмет Ч запускаЇмо таймер
            if (_cachedStack != null && _cachedStack.Item != null)
            {
                _tooltipCoroutine = StartCoroutine(ShowTooltipDelayed(_cachedStack.Item));
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // ћишка п≥шла з≥ слота Ч скасовуЇмо таймер ≥ ховаЇмо п≥дказку
            StopTooltipTimer();
        }

        // —јћ “ј…ћ≈–
        private IEnumerator ShowTooltipDelayed(ItemData item)
        {
            // „екаЇмо вказаний час
            yield return new WaitForSeconds(_tooltipDelay);

            // якщо час пройшов ≥ мишка дос≥ тут Ч показуЇмо
            TooltipUI.Instance?.Show(item);
        }

        // ƒопом≥жний метод дл€ зупинки
        private void StopTooltipTimer()
        {
            if (_tooltipCoroutine != null)
            {
                StopCoroutine(_tooltipCoroutine);
                _tooltipCoroutine = null;
            }
            TooltipUI.Instance?.Hide();
        }
    }
}