using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    [RequireComponent(typeof(ParticleSystem))]
    public class WeaponVFXAutoFit : MonoBehaviour
    {
        private void Start()
        {
            ParticleSystem ps = GetComponent<ParticleSystem>();
            var shape = ps.shape;

            // Тепер ми гарантовано прикріплені прямо до леза (наш батько)
            Collider hitbox = transform.parent.GetComponent<Collider>();

            if (hitbox != null)
            {
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.position = Vector3.zero;

                if (hitbox is BoxCollider box)
                {
                    shape.scale = box.size * 1.05f; // Робимо ефект на 5% ширшим за лезо
                }
                else if (hitbox is CapsuleCollider cap)
                {
                    shape.scale = new Vector3(cap.radius * 2, cap.height, cap.radius * 2) * 1.05f;
                }
            }
        }
    }
}