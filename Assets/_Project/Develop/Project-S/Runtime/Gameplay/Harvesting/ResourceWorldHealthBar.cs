using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project_S.Runtime.Gameplay.Harvesting
{
    [RequireComponent(typeof(HarvestableResourceNode))]
    public class ResourceWorldHealthBar : MonoBehaviour
    {
        [SerializeField] private HarvestableResourceNode _node;
        [SerializeField] private float _topPadding = 0.35f;
        [SerializeField] private float _maxHeightAboveCamera = 0.7f;
        [SerializeField] private float _minHeightAboveCamera = -0.45f;
        [SerializeField] private float _cameraPullForward = 0.18f;
        [SerializeField] private Vector2 _barSize = new Vector2(96f, 8f);

        private Transform _uiRoot;
        private RectTransform _fillRect;
        private TMP_Text _nameText;
        private Camera _camera;

        private void Awake()
        {
            if (_node == null)
                _node = GetComponent<HarvestableResourceNode>();

            enabled = false;
        }

        private void OnEnable()
        {
            if (_node != null)
                _node.HealthChanged += OnHealthChanged;

            Refresh();
        }

        private void OnDisable()
        {
            if (_node != null)
                _node.HealthChanged -= OnHealthChanged;

            if (_uiRoot != null)
                _uiRoot.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_uiRoot == null || !_uiRoot.gameObject.activeSelf)
                return;

            EnsureCamera();
            _uiRoot.position = CalculateWorldPosition();

            if (_camera != null)
                _uiRoot.rotation = Quaternion.LookRotation(_uiRoot.position - _camera.transform.position);
        }

        public void SetHovered(bool isHovered)
        {
            if (!isHovered)
            {
                if (_uiRoot != null)
                    _uiRoot.gameObject.SetActive(false);

                enabled = false;
                return;
            }

            CreateUi();

            if (_uiRoot != null)
                _uiRoot.gameObject.SetActive(true);

            if (!enabled)
                enabled = true;
            else
                Refresh();
        }

        private void CreateUi()
        {
            if (_uiRoot != null)
                return;

            var canvasObject = new GameObject("ResourceWorldHealthBar", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            _uiRoot = canvasObject.transform;

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 21;

            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(128f, 42f);
            canvasRect.localScale = Vector3.one * 0.01f;

            _nameText = CreateNameText(canvasRect);
            _fillRect = CreateHealthBar(canvasRect);
        }

        private Vector3 CalculateWorldPosition()
        {
            Bounds bounds = CalculateBounds();
            float preferredY = bounds.max.y + _topPadding;

            if (_camera != null)
            {
                float minVisibleY = Mathf.Max(bounds.min.y + _topPadding, _camera.transform.position.y + _minHeightAboveCamera);
                float maxComfortY = Mathf.Max(minVisibleY, _camera.transform.position.y + _maxHeightAboveCamera);
                preferredY = Mathf.Clamp(preferredY, minVisibleY, maxComfortY);
            }

            Vector3 position = new Vector3(bounds.center.x, preferredY, bounds.center.z);

            if (_camera != null)
            {
                Vector3 toCamera = _camera.transform.position - position;
                toCamera.y = 0f;

                if (toCamera.sqrMagnitude > 0.001f)
                    position += toCamera.normalized * _cameraPullForward;
            }

            return position;
        }

        private Bounds CalculateBounds()
        {
            bool hasBounds = false;
            Bounds bounds = new Bounds(transform.position, Vector3.zero);

            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer == null || !renderer.enabled)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            foreach (var collider in GetComponentsInChildren<Collider>())
            {
                if (collider == null || !collider.enabled)
                    continue;

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            return bounds;
        }

        private TMP_Text CreateNameText(RectTransform parent)
        {
            var textObject = new GameObject("Name", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -2f);
            rect.sizeDelta = new Vector2(128f, 22f);

            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = 17f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            return text;
        }

        private RectTransform CreateHealthBar(RectTransform parent)
        {
            var backgroundObject = new GameObject("HealthBarBackground", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(parent, false);

            var backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0.5f, 1f);
            backgroundRect.anchorMax = new Vector2(0.5f, 1f);
            backgroundRect.pivot = new Vector2(0.5f, 1f);
            backgroundRect.anchoredPosition = new Vector2(0f, -24f);
            backgroundRect.sizeDelta = _barSize;

            var background = backgroundObject.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.68f);
            background.raycastTarget = false;

            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(backgroundRect, false);

            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(1f, 1f);
            fillRect.offsetMax = new Vector2(-1f, -1f);

            var fill = fillObject.GetComponent<Image>();
            fill.color = new Color(0.85f, 0.16f, 0.08f, 0.96f);
            fill.raycastTarget = false;
            return fillRect;
        }

        private void OnHealthChanged(HarvestableResourceNode node)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_node == null)
                return;

            if (_nameText != null)
                _nameText.text = _node.Data != null ? _node.Data.NodeName : name;

            if (_fillRect != null)
                SetFill(_node.NormalizedHealth);
        }

        private void SetFill(float normalizedHealth)
        {
            normalizedHealth = Mathf.Clamp01(normalizedHealth);
            _fillRect.anchorMax = new Vector2(normalizedHealth, 1f);
            _fillRect.offsetMax = new Vector2(-1f, -1f);
        }

        private void EnsureCamera()
        {
            if (_camera != null)
                return;

            _camera = Camera.main;
            if (_camera == null)
                _camera = FindFirstObjectByType<Camera>();
        }
    }
}
