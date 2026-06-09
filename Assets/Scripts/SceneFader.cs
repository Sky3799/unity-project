using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
    [SerializeField] private Image panel;
    [SerializeField] private float duration = 0.4f;

    private void Start()
    {
        StartCoroutine(FadeIn());
    }

    // 어느 스크립트에서든 SceneFader.LoadScene("씬이름") 으로 호출
    public static void LoadScene(string sceneName)
    {
        var fader = FindFirstObjectByType<SceneFader>();
        if (fader != null)
            fader.StartCoroutine(fader.FadeOutThenLoad(sceneName));
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        SetAlpha(1f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(1f - elapsed / duration);
            yield return null;
        }
        SetAlpha(0f);
    }

    private IEnumerator FadeOutThenLoad(string sceneName)
    {
        float elapsed = 0f;
        SetAlpha(0f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(elapsed / duration);
            yield return null;
        }
        SetAlpha(1f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    private void SetAlpha(float a)
    {
        if (panel == null) return;
        var c = panel.color;
        c.a = a;
        panel.color = c;
    }
}
