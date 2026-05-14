using System.Collections;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public class TestSphereAttacker : MonoBehaviour
    {
        [Header("Налаштування зброї")]
        [SerializeField] private float _attackCooldown = 3.0f;
        [SerializeField] private float _swordLength = 2.0f;
        [SerializeField] private LayerMask _playerLayer;

        [Header("Швидкість фаз удару")]
        [SerializeField] private float _windupDuration = 0.8f; // Плавний замах назад
        [SerializeField] private float _holdDuration = 0.3f;   // Зависання перед ударом (момент готовності)
        [SerializeField] private float _strikeDuration = 0.15f;// Сам різкий удар

        [Header("Урон")]
        [SerializeField] private float _healthDamage = 30f;
        [SerializeField] private float _poiseDamage = 15f;

        private Transform _swordPivot;
        private Transform _swordVisual;
        private MeshRenderer _bladeRenderer;

        private void Start()
        {
            GameObject pivotObj = new GameObject("SwordPivot");
            _swordPivot = pivotObj.transform;
            _swordPivot.SetParent(transform);
            _swordPivot.localPosition = Vector3.zero;

            // Нейтральна позиція (палиця дивиться вперед і трохи вгору)
            _swordPivot.localRotation = Quaternion.Euler(-30f, 0f, 0f);

            GameObject visualObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualObj.name = "SwordVisual";
            Destroy(visualObj.GetComponent<Collider>());

            _swordVisual = visualObj.transform;
            _swordVisual.SetParent(_swordPivot);
            _swordVisual.localPosition = new Vector3(0f, 0f, _swordLength / 2f);
            _swordVisual.localScale = new Vector3(0.15f, 0.15f, _swordLength);

            _bladeRenderer = visualObj.GetComponent<MeshRenderer>();
            if (_bladeRenderer != null) _bladeRenderer.material.color = Color.gray;

            StartCoroutine(AttackLoop());
        }

        private IEnumerator AttackLoop()
        {
            Quaternion neutralRot = Quaternion.Euler(-30f, 0f, 0f); // Спокій
            Quaternion windupRot = Quaternion.Euler(-110f, 0f, 0f); // Замах далеко назад
            Quaternion strikeRot = Quaternion.Euler(55f, 0f, 0f);   // Удар в підлогу

            while (true)
            {
                _swordPivot.localRotation = neutralRot;
                if (_bladeRenderer != null) _bladeRenderer.material.color = Color.gray;
                yield return new WaitForSeconds(_attackCooldown);

                // --- ФАЗА 1: ЗАМАХ (Anticipation) ---
                float elapsed = 0f;
                while (elapsed < _windupDuration)
                {
                    elapsed += Time.deltaTime;
                    // Плавний відвід зброї назад
                    _swordPivot.localRotation = Quaternion.Slerp(neutralRot, windupRot, elapsed / _windupDuration);
                    yield return null;
                }

                // --- ФАЗА 2: ЗАВИСАННЯ (Hold) ---
                // Зброя завмерла вгорі. Саме зараз ти готуєшся тиснути блок
                if (_bladeRenderer != null) _bladeRenderer.material.color = new Color(1f, 0.5f, 0f); // Помаранчевий
                yield return new WaitForSeconds(_holdDuration);

                // --- ФАЗА 3: УДАР (Strike) ---
                if (_bladeRenderer != null) _bladeRenderer.material.color = Color.red;
                elapsed = 0f;
                bool damageDealt = false;

                while (elapsed < _strikeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / _strikeDuration;

                    // Блискавично опускаємо зброю
                    _swordPivot.localRotation = Quaternion.Slerp(windupRot, strikeRot, t);

                    // Дамажимо приблизно на середині траєкторії падіння
                    if (t >= 0.5f && !damageDealt)
                    {
                        damageDealt = true;
                        PerformDamageCheck();
                    }

                    yield return null;
                }

                // Пауза після удару (зброя внизу)
                yield return new WaitForSeconds(0.5f);

                // Повернення в нейтральну позицію
                elapsed = 0f;
                while (elapsed < 0.4f)
                {
                    elapsed += Time.deltaTime;
                    _swordPivot.localRotation = Quaternion.Slerp(strikeRot, neutralRot, elapsed / 0.4f);
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
                    Debug.Log("<color=green>[ВЛУЧАННЯ]</color> Палиця вдарила гравця!");
                    var request = new DamageRequest(gameObject, _healthDamage, _poiseDamage, DamageType.Slashing);
                    receiver.ReceiveDamage(request);
                }
            }
        }
    }
}