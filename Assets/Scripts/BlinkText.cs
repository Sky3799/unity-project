using System.Collections;
using UnityEngine;
using TMPro;

public class BlinkText : MonoBehaviour
{
    [SerializeField] private float minAlpha  = 0.2f;
    [SerializeField] private float maxAlpha  = 1.0f;
    [SerializeField] private float duration  = 1.2f;

    private TextMeshProUGUI _tmp;

    private void Awake() => _tmp = GetComponent<TextMeshProUGUI>();

    private void OnEnable()  => StartCoroutine(BlinkRoutine());
    private void OnDisable() => StopAllCoroutines();

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            yield return Fade(minAlpha, maxAlpha, duration * 0.5f);
            yield return Fade(maxAlpha, minAlpha, duration * 0.5f);
        }
    }

    private IEnumerator Fade(float from, float to, float time)
    {
        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / time);
            var c = _tmp.color;
            c.a = Mathf.Lerp(from, to, t);
            _tmp.color = c;
            yield return null;
        }
    }
}

