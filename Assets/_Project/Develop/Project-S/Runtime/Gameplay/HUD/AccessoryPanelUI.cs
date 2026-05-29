/*using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class AccessoryPanelUI : MonoBehaviour
    {
        private readonly List<InventorySlotUI> _slotViews = new List<InventorySlotUI>();

        private AccessorySlotController _accessories;
        private InventorySlotUI _slotPrefab;
        private Transform _slotRoot;
        private bool _built;

        public void Initialize(AccessorySlotController accessories, InventorySlotUI slotPrefab)
        {
            if (_accessories != null)
                _accessories.Changed -= Refresh;

            _accessories = accessories;
            _slotPrefab = slotPrefab;

            if (_accessories != null)
                _accessories.Changed += Refresh;

            BuildLayout();
            Refresh();
        }

        public void Refresh()
        {
            if (!_built || _accessories == null)
                return;

            int size = _accessories.GetSize();
            EnsureSlotViews(size);

            for (int i = 0; i < _slotViews.Count; i++)
            {
                var item = _accessories.GetItemInSlot(i);
                _slotViews[i].UpdateView(item != null ? new ItemStack(item, 1) : null);
            }
        }

        private void OnAccessorySlotClicked(int slotIndex, PointerEventData.InputButton button)
        {
            if (button != PointerEventData.InputButton.Left && button != PointerEventData.InputButton.Right)
                return;

            _accessories?.TryUnequipToInventory(slotIndex);
            Refresh();
        }

        private void EnsureSlotViews(int count)
        {
            if (_slotRoot == null || _slotPrefab == null)
                return;

            while (_slotViews.Count < count)
            {
                int index = _slotViews.Count;
                var slot = Instantiate(_slotPrefab, _slotRoot);
                slot.Init(index, null, OnAccessorySlotClicked);
                _slotViews.Add(slot);
            }

            while (_slotViews.Count > count)
            {
                int lastIndex = _slotViews.Count - 1;
                var slot = _slotViews[lastIndex];
                _slotViews.RemoveAt(lastIndex);
                if (slot != null)
                    Destroy(slot.gameObject);
            }
        }

        private void BuildLayout()
        {
            if (_built)
                return;

            _built = true;

            var root = CreateRect("AccessoryPanelRoot", transform);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.offsetMin = new Vector2(8f, -92f);
            root.offsetMax = new Vector2(-8f, -8f);

            var image = root.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.12f);

            var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = CreateText(root, "AccessoriesTitle", 14, FontStyles.Bold);
            title.text = "Accessories";

            _slotRoot = CreateRect("AccessorySlots", root);
            var slotLayout = _slotRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            slotLayout.spacing = 6f;
            slotLayout.childControlWidth = false;
            slotLayout.childControlHeight = false;
            slotLayout.childForceExpandWidth = false;
            slotLayout.childForceExpandHeight = false;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static TMP_Text CreateText(Transform parent, string name, int size, FontStyles style)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = size + 4f;
            return text;
        }

        private void OnDestroy()
        {
            if (_accessories != null)
                _accessories.Changed -= Refresh;
        }
    }
}
*/

using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class AccessoryPanelUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform _slotRoot; // Сюди ти перетягнеш сітку з Інспектора

        private readonly List<InventorySlotUI> _slotViews = new List<InventorySlotUI>();
        private AccessorySlotController _accessories;
        private InventorySlotUI _slotPrefab;

        public void Initialize(AccessorySlotController accessories, InventorySlotUI slotPrefab)
        {
            if (_accessories != null)
                _accessories.Changed -= Refresh;

            _accessories = accessories;
            _slotPrefab = slotPrefab;

            if (_accessories != null)
                _accessories.Changed += Refresh;

            Refresh();
        }

        public void Refresh()
        {
            if (_accessories == null || _slotRoot == null)
                return;

            int size = _accessories.GetSize();
            EnsureSlotViews(size);

            for (int i = 0; i < _slotViews.Count; i++)
            {
                var item = _accessories.GetItemInSlot(i);
                _slotViews[i].UpdateView(item != null ? new ItemStack(item, 1) : null);
            }
        }

        private void OnAccessorySlotClicked(int slotIndex, PointerEventData.InputButton button)
        {
            if (button != PointerEventData.InputButton.Left && button != PointerEventData.InputButton.Right)
                return;

            _accessories?.TryUnequipToInventory(slotIndex);
            Refresh();
        }

        private void EnsureSlotViews(int count)
        {
            if (_slotRoot == null || _slotPrefab == null)
                return;

            while (_slotViews.Count < count)
            {
                int index = _slotViews.Count;
                var slot = Instantiate(_slotPrefab, _slotRoot);
                slot.Init(index, null, OnAccessorySlotClicked);
                _slotViews.Add(slot);
            }

            while (_slotViews.Count > count)
            {
                int lastIndex = _slotViews.Count - 1;
                var slot = _slotViews[lastIndex];
                _slotViews.RemoveAt(lastIndex);
                if (slot != null)
                    Destroy(slot.gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_accessories != null)
                _accessories.Changed -= Refresh;
        }
    }
}