using System.Collections;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public class TestSphereAttacker : MonoBehaviour
    {
        [Header("Ќалаштуванн€ зброњ")]
        [SerializeField] private float _attackCooldown = 2.5f; // –аз на ск≥льки секунд б'Ї
        [SerializeField] private float _swingDuration = 0.25f; // Ўвидк≥сть самого удару (дуже швидкий випад!)
        [SerializeField] private float _swordLength = 2.0f;    // ƒовжина палиц≥
        [SerializeField] private LayerMask _playerLayer;       // Ўар гравц€

        [Header("”рон")]
        [SerializeField] private float _healthDamage = 30f;
        [SerializeField] private float _poiseDamage = 15f;

        private Transform _swordPivot;
        private Transform _swordVisual;

        private void Start()
        {
            GameObject pivotObj = new GameObject("SwordPivot");
            _swordPivot = pivotObj.transform;
            _swordPivot.SetParent(transform);
            _swordPivot.localPosition = Vector3.zero;
            _swordPivot.localRotation = Quaternion.Euler(-75f, 0f, 0f);

            GameObject visualObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualObj.name = "SwordVisual";
            Destroy(visualObj.GetComponent<Collider>());

            _swordVisual = visualObj.transform;
            _swordVisual.SetParent(_swordPivot);
            _swordVisual.localPosition = new Vector3(0f, 0f, _swordLength / 2f);
            _swordVisual.localScale = new Vector3(0.15f, 0.15f, _swordLength);

            if (visualObj.TryGetComponent<MeshRenderer>(out var rend))
                rend.material.color = Color.gray;

            StartCoroutine(AttackLoop());
        }

        private IEnumerator AttackLoop()
        {
            while (true)
            {
                _swordPivot.localRotation = Quaternion.Euler(-75f, 0f, 0f);
                yield return new WaitForSeconds(_attackCooldown);

                float elapsed = 0f;
                Quaternion startRot = Quaternion.Euler(-75f, 0f, 0f); 
                Quaternion endRot = Quaternion.Euler(55f, 0f, 0f);    

                bool damageDealt = false;

                if (_swordVisual.TryGetComponent<MeshRenderer>(out var rend))
                    rend.material.color = Color.white;

                while (elapsed < _swingDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / _swingDuration;

                    _swordPivot.localRotation = Quaternion.Slerp(startRot, endRot, t);

                    if (t >= 0.5f && !damageDealt)
                    {
                        damageDealt = true;
                        PerformDamageCheck();
                    }

                    yield return null;
                }

                if (rend != null) rend.material.color = Color.gray;

                yield return new WaitForSeconds(0.5f);

                float returnElapsed = 0f;
                Quaternion currentRot = _swordPivot.localRotation;
                while (returnElapsed < 0.4f)
                {
                    returnElapsed += Time.deltaTime;
                    _swordPivot.localRotation = Quaternion.Slerp(currentRot, startRot, returnElapsed / 0.4f);
                    yield return null;
                }
            }
        }

        private void PerformDamageCheck()
        {
            Vector3 startPoint = _swordPivot.position;
            Vector3 endPoint = _swordPivot.position + (_swordPivot.forward * _swordLength);

            Collider[] hits = Physics.OverlapCapsule(startPoint, endPoint, 0.4f, _playerLayer);

            foreach (var hit in hits)
            {
                var receiver = hit.GetComponentInParent<CharacterDamageReceiver>();

                if (receiver != null)
                {
                    Debug.Log("<color=green>[¬Ћ”„јЌЌя]</color> ѕалиц€ вдарила гравц€!");

                    var request = new DamageRequest(gameObject, _healthDamage, _poiseDamage, DamageType.Slashing);

                    receiver.ReceiveDamage(request);
                }
            }
        }
    }
}