using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Input
{
    public class KeyboardPlayerInput : MonoBehaviour, IPlayerInput
    {
        [SerializeField] private string _horizontalAxis = "Horizontal";
        [SerializeField] private string _verticalAxis = "Vertical";
        [SerializeField] private string _mouseXAxis = "Mouse X";
        [SerializeField] private string _mouseYAxis = "Mouse Y";
        [SerializeField] private KeyCode _sprintKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode _jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode _dodgeKey = KeyCode.LeftAlt;
        [SerializeField] private KeyCode _interactKey = KeyCode.E;

        public PlayerInputSnapshot Snapshot { get; private set; }

        private void Update()
        {
            Snapshot = new PlayerInputSnapshot(
                new Vector2(UnityEngine.Input.GetAxisRaw(_horizontalAxis), UnityEngine.Input.GetAxisRaw(_verticalAxis)),
                new Vector2(UnityEngine.Input.GetAxisRaw(_mouseXAxis), UnityEngine.Input.GetAxisRaw(_mouseYAxis)),
                UnityEngine.Input.GetKey(_sprintKey),
                UnityEngine.Input.GetKeyDown(_jumpKey),
                UnityEngine.Input.GetKeyDown(_dodgeKey),
                UnityEngine.Input.GetMouseButton(1),
                UnityEngine.Input.GetMouseButtonDown(0),
                UnityEngine.Input.GetMouseButton(0) && UnityEngine.Input.GetMouseButtonDown(1),
                UnityEngine.Input.GetKeyDown(_interactKey));
        }
    }
}
