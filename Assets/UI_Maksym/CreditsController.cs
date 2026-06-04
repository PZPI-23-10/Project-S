using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class CreditsController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private RectTransform _creditsContainer;
    
    [Header("Settings")]
    [SerializeField] private string _menuSceneName = "MainMenu";
    [SerializeField] private float _fadeDuration = 5f;
    [SerializeField] private float _holdTitleDuration = 2f;
    [SerializeField] private float _scrollSpeed = 80f;
    [SerializeField] private float _endYPosition = 3000f; 

    private bool _isScrolling = false;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        StartCoroutine(CreditsSequence());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LoadMainMenu();
        }

        if (_isScrolling && _creditsContainer != null)
        {
            _creditsContainer.anchoredPosition += Vector2.up * _scrollSpeed * Time.deltaTime;

            if (_creditsContainer.anchoredPosition.y >= _endYPosition)
            {
                _isScrolling = false;
                LoadMainMenu();
            }
        }
    }

    private IEnumerator CreditsSequence()
    {
        if (_titleText == null || _creditsContainer == null) yield break;

        // Починаємо титри ще нижче екрану, щоб спочатку їх не було видно
        _creditsContainer.anchoredPosition = new Vector2(0, -300f);
        _titleText.color = new Color(1, 1, 1, 1);

        // Fade in хвилею зліва направо
        yield return StartCoroutine(AnimateTitleWave(true));

        // Hold Title
        yield return new WaitForSeconds(_holdTitleDuration);

        // Fade out хвилею зліва направо
        yield return StartCoroutine(AnimateTitleWave(false));

        // Start scrolling
        _isScrolling = true;
    }

    private IEnumerator AnimateTitleWave(bool fadeIn)
    {
        _titleText.ForceMeshUpdate();
        TMP_TextInfo textInfo = _titleText.textInfo;

        float minX = float.MaxValue;
        float maxX = float.MinValue;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;
            Vector3 center = (textInfo.characterInfo[i].bottomLeft + textInfo.characterInfo[i].topRight) / 2f;
            if (center.x < minX) minX = center.x;
            if (center.x > maxX) maxX = center.x;
        }

        float t = 0;
        while (t < _fadeDuration)
        {
            t += Time.deltaTime;
            float progress = t / _fadeDuration;
            
            float sweepPos = Mathf.Lerp(minX - 300f, maxX + 300f, progress);

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible) continue;
                
                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

                Vector3 center = (textInfo.characterInfo[i].bottomLeft + textInfo.characterInfo[i].topRight) / 2f;

                float alpha = Mathf.Clamp01((sweepPos - center.x) / 300f);
                if (!fadeIn) alpha = 1f - alpha;

                byte a = (byte)(alpha * 255);
                colors[vertexIndex + 0].a = a;
                colors[vertexIndex + 1].a = a;
                colors[vertexIndex + 2].a = a;
                colors[vertexIndex + 3].a = a;
            }
            
            _titleText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            yield return null;
        }
    }

    private void LoadMainMenu()
    {
        SceneManager.LoadScene(_menuSceneName);
    }
}
