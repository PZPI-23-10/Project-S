using System;
using System.Collections;
using Project_S.Runtime.Gameplay.Character.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project_S.Runtime.Gameplay.HUD
{
    public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _amountText;
        [SerializeField] private float _tooltipDelay = 0.5f;

        [Header("Selection")]
        [SerializeField] private RectTransform _selectionHighlight;
        [SerializeField] private Color _selectionFillColor = new Color(1f, 0.82f, 0.2f, 0.14f);
        [SerializeField] private Color _selectionOutlineColor = new Color(1f, 0.86f, 0.28f, 1f);
        [SerializeField] private float _selectionPadding = 4f;

        private int _slotIndex;
        private InventoryUI _uiManager;
        private ItemStack _cachedStack;
        private Coroutine _tooltipCoroutine;
        private RectTransform _rectTransform;
        private Action<int, PointerEventData.InputButton> _clickOverride;
#if UNITY_EDITOR
        private bool _editorEnsureQueued;
#endif

        public void Init(int slotIndex, InventoryUI uiManager, Action<int, PointerEventData.InputButton> clickOverride = null)
        {
            _slotIndex = slotIndex;
            _uiManager = uiManager;
            _clickOverride = clickOverride;
            _rectTransform = transform as RectTransform;
            SetSelected(false);
        }

        public void UpdateView(ItemStack stack)
        {
            _cachedStack = stack;

            if (stack == null || stack.Item == null)
            {
                if (_iconImage != null)
                    _iconImage.gameObject.SetActive(false);

                if (_amountText != null)
                    _amountText.gameObject.SetActive(false);

                StopTooltipTimer();
                return;
            }

            if (_iconImage != null)
            {
                if (stack.Item.Icon != null)
                {
                    _iconImage.sprite = stack.Item.Icon;
                    _iconImage.gameObject.SetActive(true);
                }
                else
                {
                    _iconImage.gameObject.SetActive(false);
                }
            }

            if (_amountText != null)
            {
                _amountText.text = stack.Amount > 1 ? stack.Amount.ToString() : string.Empty;
                _amountText.gameObject.SetActive(stack.Amount > 1);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            StopTooltipTimer();
            if (_clickOverride != null)
                _clickOverride.Invoke(_slotIndex, eventData.button);
            else
                _uiManager?.OnSlotClicked(_slotIndex, eventData.button);
        }

        public void SetSelected(bool selected)
        {
            if (selected)
                EnsureSelectionHighlight();

            if (_selectionHighlight != null)
                _selectionHighlight.gameObject.SetActive(selected);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_cachedStack != null && _cachedStack.Item != null)
                _tooltipCoroutine = StartCoroutine(ShowTooltipDelayed(_cachedStack.Item));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StopTooltipTimer();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying || _editorEnsureQueued)
                return;

            _editorEnsureQueued = true;
            EditorApplication.delayCall += EnsureSelectionHighlightInEditor;
        }
#endif

        private IEnumerator ShowTooltipDelayed(ItemData item)
        {
            yield return new WaitForSeconds(_tooltipDelay);

            if (_cachedStack == null || _cachedStack.Item != item)
                yield break;

            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;

            TooltipUI.GetOrCreate()?.Show(item, _rectTransform);
            _tooltipCoroutine = null;
        }

        private void StopTooltipTimer()
        {
            if (_tooltipCoroutine != null)
            {
                StopCoroutine(_tooltipCoroutine);
                _tooltipCoroutine = null;
            }

            TooltipUI.Instance?.Hide();
        }

#if UNITY_EDITOR
        private void EnsureSelectionHighlightInEditor()
        {
            _editorEnsureQueued = false;
            if (this == null || Application.isPlaying)
                return;

            EnsureSelectionHighlight();
            if (_selectionHighlight != null)
                _selectionHighlight.gameObject.SetActive(false);

            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(gameObject);
        }
#endif

        private void EnsureSelectionHighlight()
        {
            if (_selectionHighlight == null)
            {
                var highlightObject = new GameObject("SelectionHighlight", typeof(RectTransform), typeof(Image), typeof(Outline));
                highlightObject.transform.SetParent(transform, false);
                _selectionHighlight = highlightObject.GetComponent<RectTransform>();
            }
            else if (_selectionHighlight.parent != transform)
            {
                _selectionHighlight.SetParent(transform, false);
            }

            ConfigureSelectionHighlightRect();

            if (!_selectionHighlight.TryGetComponent(out Image image))
                image = _selectionHighlight.gameObject.AddComponent<Image>();

            image.raycastTarget = false;
            image.color = _selectionFillColor;

            if (!_selectionHighlight.TryGetComponent(out Outline outline))
                outline = _selectionHighlight.gameObject.AddComponent<Outline>();

            outline.effectColor = _selectionOutlineColor;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = false;
        }

        private void ConfigureSelectionHighlightRect()
        {
            _selectionHighlight.anchorMin = Vector2.zero;
            _selectionHighlight.anchorMax = Vector2.one;
            _selectionHighlight.pivot = new Vector2(0.5f, 0.5f);
            _selectionHighlight.offsetMin = new Vector2(-_selectionPadding, -_selectionPadding);
            _selectionHighlight.offsetMax = new Vector2(_selectionPadding, _selectionPadding);
            _selectionHighlight.localScale = Vector3.one;
            _selectionHighlight.localRotation = Quaternion.identity;
            _selectionHighlight.SetAsFirstSibling();
        }
    }
}
