using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Interaction
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float _interactDistance = 2.5f;
        [SerializeField] private InventoryController _inventory;

        // Виправлено: жорстко вказуємо UnityEngine.Camera, щоб уникнути конфлікту
        private UnityEngine.Camera _cam;

        private void Awake()
        {
            _cam = GetComponent<UnityEngine.Camera>();
            if (_inventory == null) _inventory = GetComponentInParent<InventoryController>();
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.E))
            {
                Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);

                if (Physics.Raycast(ray, out RaycastHit hit, _interactDistance))
                {
                    if (hit.collider.TryGetComponent(out ItemPickup pickup))
                    {
                        pickup.Collect(_inventory);
                    }
                }
            }
        }
    }
}