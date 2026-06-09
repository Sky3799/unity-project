using System.Collections;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    private TextMeshProUGUI label;

    private void Awake() => label = GetComponent<TextMeshProUGUI>();

    public static DamagePopup Create(GameObject prefab, Transform canvasTransform, Vector2 anchoredPos, string text, Color color)
    {
        var go = Instantiate(prefab, canvasTransform);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;

        var popup = go.GetComponent<DamagePopup>();
        popup.label.text = text;
        popup.label.color = color;
        popup.StartCoroutine(popup.Animate());
        return popup;
    }

    private IEnumerator Animate()
    {
        float duration = 1.2f;
        float fadeStart = 0.5f;
        float elapsed = 0f;
        float riseAmount = 120f;

        Vector2 startPos = GetComponent<RectTransform>().anchoredPosition;
        Color baseColor = label.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            GetComponent<RectTransform>().anchoredPosition = startPos + Vector2.up * (riseAmount * t);

            float alpha = elapsed > fadeStart ? 1f - ((elapsed - fadeStart) / (duration - fadeStart)) : 1f;
            label.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            yield return null;
        }

        Destroy(gameObject);
    }
}

