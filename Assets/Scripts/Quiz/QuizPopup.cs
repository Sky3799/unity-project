using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 카드 사용 시 표시되는 3지선다 퀴즈 팝업 UI
/// SetActive 대신 CanvasGroup으로 표시/숨김 처리 — 항상 활성 상태를 유지해 코루틴 안전
/// </summary>
public class QuizPopup : MonoBehaviour
{
    [Header("레이아웃 오브젝트")]
    [SerializeField] private GameObject overlay;                        // 전체 화면 반투명 오버레이
    [SerializeField] private GameObject popupBox;                       // 중앙 팝업 박스

    [Header("텍스트 UI")]
    [SerializeField] private TextMeshProUGUI cardActivationText;        // "「카드이름」 발동!" 텍스트
    [SerializeField] private TextMeshProUGUI questionText;              // 문제 텍스트

    [Header("보기 버튼 (3개)")]
    [SerializeField] private Button[] answerButtons = new Button[3];    // 3지선다 버튼
    [SerializeField] private TextMeshProUGUI[] answerTexts = new TextMeshProUGUI[3]; // 버튼 레이블

    [Header("타이머")]
    [SerializeField] private Image timerFillImage;                      // 원형 게이지 (fillAmount)
    [SerializeField] private TextMeshProUGUI timerText;                 // 남은 초 숫자
    [SerializeField] private float timeLimit = 10f;                     // 제한 시간(초)

    [Header("결과 패널")]
    [SerializeField] private GameObject resultPanel;                    // 정답/오답 결과 패널
    [SerializeField] private TextMeshProUGUI resultText;                // "정답!" / "오답!" 텍스트
    [SerializeField] private TextMeshProUGUI correctAnswerRevealText;   // 오답 시 정답 표시

    [Header("색상")]
    [SerializeField] private Color correctColor = new Color(0.18f, 0.8f, 0.3f);
    [SerializeField] private Color wrongColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color selectedCorrectBtnColor = new Color(0.3f, 0.9f, 0.4f);
    [SerializeField] private Color selectedWrongBtnColor = new Color(0.9f, 0.3f, 0.3f);

    private Action<bool> onComplete;
    private string correctAnswer;
    private Coroutine timerCoroutine;
    private bool answered;
    private CanvasGroup overlayCanvasGroup;

    private void Awake()
    {
        // CanvasGroup으로 숨김 처리 — SetActive 사용하지 않아 코루틴 항상 시작 가능
        if (overlay != null)
        {
            overlayCanvasGroup = overlay.GetComponent<CanvasGroup>();
            if (overlayCanvasGroup == null)
                overlayCanvasGroup = overlay.AddComponent<CanvasGroup>();
        }
        SetOverlayVisible(false);
    }

    // ─── 공개 API ───────────────────────────────────────────────────

    /// <summary>
    /// 퀴즈 팝업을 열고 문제를 표시한다
    /// </summary>
    public void ShowQuiz(string cardName, string question, string[] answers,
        string correct, Action<bool> callback)
    {
        answered = false;
        correctAnswer = correct;
        onComplete = callback;

        SetOverlayVisible(true);
        if (resultPanel != null) resultPanel.SetActive(false);

        cardActivationText.text = $"「{cardName}」 발동!";
        questionText.text = question;

        // 보기 버튼 설정
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int idx = i;
            answerTexts[i].text = answers[i];
            answerButtons[i].interactable = true;
            SetButtonColor(answerButtons[i], Color.white);
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(answers[idx], answerButtons[idx]));
        }

        // 타이머 시작
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(TimerRoutine());
    }

    /// <summary>팝업 강제 닫기</summary>
    public void ForceClose()
    {
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        SetOverlayVisible(false);
    }

    // ─── 내부 처리 ──────────────────────────────────────────────────

    private void OnAnswerSelected(string answer, Button clickedButton)
    {
        if (answered) return;
        answered = true;

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        bool isCorrect = answer == correctAnswer;
        SetButtonColor(clickedButton, isCorrect ? selectedCorrectBtnColor : selectedWrongBtnColor);

        foreach (var btn in answerButtons)
            btn.interactable = false;

        ShowResult(isCorrect);
    }

    private void ShowResult(bool isCorrect)
    {
        if (resultPanel != null) resultPanel.SetActive(true);

        if (resultText != null)
        {
            resultText.text = isCorrect ? "정답!" : "오답!";
            resultText.color = isCorrect ? correctColor : wrongColor;
        }

        if (correctAnswerRevealText != null)
        {
            correctAnswerRevealText.gameObject.SetActive(!isCorrect);
            if (!isCorrect)
                correctAnswerRevealText.text = $"정답: {correctAnswer}";
        }

        StartCoroutine(CloseAfterDelay(isCorrect, 1.8f));
    }

    private IEnumerator CloseAfterDelay(bool isCorrect, float delay)
    {
        yield return new WaitForSeconds(delay);
        SetOverlayVisible(false);
        onComplete?.Invoke(isCorrect);
    }

    private IEnumerator TimerRoutine()
    {
        float elapsed = 0f;

        while (elapsed < timeLimit)
        {
            elapsed += Time.deltaTime;
            float ratio = 1f - (elapsed / timeLimit);
            if (timerFillImage != null) timerFillImage.fillAmount = ratio;
            if (timerText != null) timerText.text = Mathf.CeilToInt(timeLimit - elapsed).ToString();
            yield return null;
        }

        // 시간 초과 → 오답 처리
        if (!answered)
        {
            answered = true;
            foreach (var btn in answerButtons)
                btn.interactable = false;
            ShowResult(false);
        }
    }

    // CanvasGroup으로 오버레이 표시/숨김 (오브젝트는 항상 활성 상태 유지)
    private void SetOverlayVisible(bool visible)
    {
        if (overlayCanvasGroup == null) return;
        overlayCanvasGroup.alpha = visible ? 1f : 0f;
        overlayCanvasGroup.interactable = visible;
        overlayCanvasGroup.blocksRaycasts = visible;
    }

    private void SetButtonColor(Button btn, Color color)
    {
        var colors = btn.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.1f;
        btn.colors = colors;
    }
}
