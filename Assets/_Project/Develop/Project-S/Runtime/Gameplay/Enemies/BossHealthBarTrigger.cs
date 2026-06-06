using Project_S.Runtime.Gameplay.HUD;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Enemies
{
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(EnemyController))]
    public class BossHealthBarTrigger : MonoBehaviour
    {
        [Header("UI Settings")]
        [SerializeField] private string _bossName = "Boss Name";
        [SerializeField] private Color _fillColor = Color.red;
        
        [Header("Bar Dimensions (RectTransform)")]
        [SerializeField] private float _barWidth = 800f;
        [SerializeField] private float _barHeight = 40f;
        [SerializeField] private Vector3 _barScale = Vector3.one;

        private EnemyHealth _health;
        private EnemyController _controller;
        private bool _isShowing;

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _controller = GetComponent<EnemyController>();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.HealthChanged += OnHealthChanged;
                _health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.HealthChanged -= OnHealthChanged;
                _health.Died -= OnDied;
            }

            if (_isShowing)
                HideBar();
        }

        private void Update()
        {
            if (_health == null || _controller == null || _health.IsDead)
                return;

            if (_controller.HasAggro && !_isShowing)
            {
                ShowBar();
            }
            else if (!_controller.HasAggro && _isShowing)
            {
                HideBar();
            }
        }

        private void ShowBar()
        {
            if (BossHealthBarUI.Instance == null)
                return;

            _isShowing = true;
            BossHealthBarUI.Instance.Show(_bossName, _health.NormalizedHealth, _fillColor, _barWidth, _barHeight, _barScale);
        }

        private void HideBar()
        {
            _isShowing = false;
            
            if (BossHealthBarUI.Instance != null)
                BossHealthBarUI.Instance.Hide();
        }

        private void OnHealthChanged(EnemyHealth health)
        {
            if (_isShowing && BossHealthBarUI.Instance != null)
            {
                BossHealthBarUI.Instance.UpdateHealth(health.NormalizedHealth);
            }
        }

        private void OnDied(EnemyHealth health)
        {
            if (_isShowing)
                HideBar();
        }
    }
}
