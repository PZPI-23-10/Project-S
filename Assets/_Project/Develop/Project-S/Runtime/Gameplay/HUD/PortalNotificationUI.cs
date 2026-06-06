using System.Collections;
using TMPro;
using UnityEngine;
using Project_S.Runtime.Gameplay.Portals;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class PortalNotificationUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text _notificationText;

        [Header("Settings")]
        [SerializeField] private float _showDuration = 3f;
        [Tooltip("Якщо > 0, ігнорує реальну кількість порталів на сцені та показує це значення.")]
        [SerializeField] private int _totalPortalsOverride = 3;

        private Coroutine _hideCoroutine;

        private void Awake()
        {
            if (_notificationText != null)
                _notificationText.enabled = false;
        }

        private void OnEnable()
        {
            PortalCompletionManager.PortalClosedProgress += OnPortalClosed;
        }

        private void OnDisable()
        {
            PortalCompletionManager.PortalClosedProgress -= OnPortalClosed;
        }

        private void OnPortalClosed(int closedCount, int totalCount)
        {
            if (_notificationText == null) return;

            // Використовуємо значення з інспектора, якщо воно більше 0, інакше беремо реальне зі сцени
            int total = _totalPortalsOverride > 0 ? _totalPortalsOverride : totalCount;

            _notificationText.text = $"Портал закрито {closedCount}/{total}";
            _notificationText.enabled = true;

            if (_hideCoroutine != null)
                StopCoroutine(_hideCoroutine);

            _hideCoroutine = StartCoroutine(HideAfterDelay(_showDuration));
        }

        private IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (_notificationText != null)
                _notificationText.enabled = false;
        }
    }
}
