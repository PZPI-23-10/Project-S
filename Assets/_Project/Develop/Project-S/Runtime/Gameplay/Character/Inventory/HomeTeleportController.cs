using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Inventory
{
    public class HomeTeleportController : MonoBehaviour
    {
        private Vector3 _homePosition;
        private bool _hasHomePosition;
        private float _remainingSeconds;

        public bool IsTeleporting => _remainingSeconds > 0f;
        public Vector3 HomePosition => _homePosition;

        private void Awake()
        {
            SetHomePosition(transform.position);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void SetHomePosition(Vector3 position)
        {
            _homePosition = position;
            _hasHomePosition = true;
        }

        public void StartTeleport(float delaySeconds)
        {
            if (!_hasHomePosition)
                SetHomePosition(transform.position);

            _remainingSeconds = Mathf.Max(0f, delaySeconds);
            if (_remainingSeconds <= 0f)
                CompleteTeleport();
        }

        public void Tick(float deltaTime)
        {
            if (_remainingSeconds <= 0f || deltaTime <= 0f)
                return;

            _remainingSeconds = Mathf.Max(0f, _remainingSeconds - deltaTime);
            if (_remainingSeconds <= 0f)
                CompleteTeleport();
        }

        private void CompleteTeleport()
        {
            transform.position = _homePosition;
            _remainingSeconds = 0f;
        }
    }
}
