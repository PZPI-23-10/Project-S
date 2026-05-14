using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class QteUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _qteText;
        [SerializeField] private Slider _balanceSlider;

        private void Awake()
        {
            HideCompletely();
        }

        private void Start()
        {
            HideCompletely();
        }

        public void UpdateUI(float currentPoise, float maxPoise, bool isQTE, KeyCode currentKey)
        {
            // --- √ќЋќ¬Ќ≈ ¬»ѕ–ј¬Ћ≈ЌЌя ---
            // якщо ми Ќ≈ в стан≥ оглушенн€/пор€тунку (QTE), смужка ≥ текст взагал≥ не повинн≥ з'€вл€тис€ на екран≥
            if (!isQTE)
            {
                HideCompletely();
                return;
            }

            // ¬микаЇмо ≥нтерфейс т≥льки тод≥, коли гравцев≥ реально треба клацати кнопку
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            if (_balanceSlider != null)
            {
                if (!_balanceSlider.gameObject.activeSelf) _balanceSlider.gameObject.SetActive(true);
                _balanceSlider.value = currentPoise / maxPoise;
            }

            if (_qteText != null)
            {
                if (!_qteText.gameObject.activeSelf) _qteText.gameObject.SetActive(true);
                _qteText.text = currentKey.ToString();
            }
        }

        private void HideCompletely()
        {
            if (_balanceSlider != null && _balanceSlider.gameObject.activeSelf)
                _balanceSlider.gameObject.SetActive(false);

            if (_qteText != null && _qteText.gameObject.activeSelf)
                _qteText.gameObject.SetActive(false);

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }
    }
}