using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageClearOverlay : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private GameObject    overlayPanel;
    [SerializeField] private Image         illustrationImage;
    [SerializeField] private TextMeshProUGUI clearText;

    [Header("설정")]
    [SerializeField] private float displayDuration = 6f;

    public void ShowClear(Sprite illustration, string stageText, Action onComplete)
    {
        StartCoroutine(ShowRoutine(illustration, stageText, onComplete));
    }

    private IEnumerator ShowRoutine(Sprite sprite, string text, Action onComplete)
    {
        if (overlayPanel      != null) overlayPanel.SetActive(true);
        if (illustrationImage != null) illustrationImage.sprite = sprite;
        if (clearText         != null) clearText.text = text;

        // 페이드 인
        yield return FadePanel(0f, 1f, 0.4f);
        yield return new WaitForSeconds(displayDuration - 0.8f);
        // 페이드 아웃
        yield return FadePanel(1f, 0f, 0.4f);

        if (overlayPanel != null) overlayPanel.SetActive(false);
        onComplete?.Invoke();
    }

    private IEnumerator FadePanel(float from, float to, float time)
    {
        var cg = overlayPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = overlayPanel.AddComponent<CanvasGroup>();
        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / time);
            yield return null;
        }
        cg.alpha = to;
    }
}

