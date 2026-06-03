using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project_S.Runtime.Gameplay.Enemies
{
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyWorldHealthBar : MonoBehaviour
    {
        [SerializeField] private EnemyHealth _health;
        [SerializeField] private string _displayName = "Скелет";
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 1.1f, 0f);
        [SerializeField] private Vector2 _barSize = new Vector2(92f, 8f);

        private Transform _uiRoot;
        private RectTransform _fillRect;
        private TMP_Text _nameText;
        private Camera _camera;

        private void Awake()
        {
            if (_health == null)
                _health = GetComponent<EnemyHealth>();

            CreateUi();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.HealthChanged += OnHealthChanged;
                _health.Died += OnDied;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.HealthChanged -= OnHealthChanged;
                _health.Died -= OnDied;
            }
        }

        private void LateUpdate()
        {
            if (_uiRoot == null)
                return;

            _uiRoot.position = transform.position + _worldOffset;
            EnsureCamera();

            if (_camera != null)
                _uiRoot.rotation = Quaternion.LookRotation(_uiRoot.position - _camera.transform.position);
        }

        public void Configure(string displayName, Vector3 worldOffset)
        {
            _displayName = displayName;
            _worldOffset = worldOffset;
            Refresh();
        }

        private void CreateUi()
        {
            var canvasObject = new GameObject("EnemyWorldHealthBar", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform);
            _uiRoot = canvasObject.transform;
            _uiRoot.localPosition = _worldOffset;

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;

            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(120f, 40f);
            canvasRect.localScale = Vector3.one * 0.01f;

            _nameText = CreateNameText(canvasRect);
            _fillRect = CreateHealthBar(canvasRect);
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
            rect.sizeDelta = new Vector2(120f, 22f);

            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = _displayName;
            text.fontSize = 18f;
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
            background.color = new Color(0f, 0f, 0f, 0.65f);
            background.raycastTarget = false;

            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(backgroundRect, false);

            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(1f, 1f);
            fillRect.offsetMax = new Vector2(-1f, -1f);

            var fill = fillObject.GetComponent<Image>();
            fill.color = new Color(0.85f, 0.08f, 0.08f, 0.95f);
            fill.raycastTarget = false;
            return fillRect;
        }

        private void Refresh()
        {
            if (_nameText != null)
                _nameText.text = _displayName;

            if (_fillRect != null && _health != null)
                SetFill(_health.NormalizedHealth);
        }

        private void OnHealthChanged(EnemyHealth health)
        {
            Refresh();
        }

        private void OnDied(EnemyHealth health)
        {
            if (_uiRoot != null)
                _uiRoot.gameObject.SetActive(false);
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
