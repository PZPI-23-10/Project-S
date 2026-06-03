using System.Collections;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Harvesting
{
    public class TreeFellingAnimation : MonoBehaviour, IResourceDepletionHandler
    {
        [SerializeField] private float _fallDuration = 1.15f;
        [SerializeField] private float _fallAngle = 82f;
        [SerializeField] private float _lingerSeconds = 1.25f;

        private bool _falling;

        public void HandleResourceDepleted(HarvestableResourceNode node)
        {
            if (_falling)
                return;

            if (!Application.isPlaying)
            {
                DestroyImmediate(gameObject);
                return;
            }

            StartCoroutine(FallRoutine(node));
        }

        private IEnumerator FallRoutine(HarvestableResourceNode node)
        {
            _falling = true;

            Quaternion startRotation = transform.rotation;
            Vector3 direction = transform.forward;

            if (node != null && node.Data != null)
                direction = Quaternion.Euler(0f, Random.Range(-35f, 35f), 0f) * direction;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector3.forward;

            direction.Normalize();
            Vector3 fallAxis = Vector3.Cross(Vector3.up, direction).normalized;
            Quaternion endRotation = Quaternion.AngleAxis(_fallAngle, fallAxis) * startRotation;
            float duration = Mathf.Max(0.01f, _fallDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                transform.rotation = Quaternion.Slerp(startRotation, endRotation, eased);
                yield return null;
            }

            transform.rotation = endRotation;

            if (_lingerSeconds > 0f)
                yield return new WaitForSeconds(_lingerSeconds);

            Destroy(gameObject);
        }
    }
}
