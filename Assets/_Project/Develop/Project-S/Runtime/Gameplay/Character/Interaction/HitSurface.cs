using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public class HitSurface : MonoBehaviour
    {
        [Tooltip("ўо спавнити при удар≥")]
        public GameObject HitVFXPrefab;

        [Tooltip("«вук удару")]
        public AudioClip SurfaceHitSound;
    }
}