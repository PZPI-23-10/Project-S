using UnityEngine;

namespace Project_S.Runtime.Core.Services
{
    public class CameraProvider : MonoBehaviour
    {
        [field: SerializeField] public Camera MainCamera { get; private set; }
    }
}