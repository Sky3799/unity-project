using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class WrongNoteManager : MonoBehaviour
{
    [Header("스크롤 컨텐츠")]
    [SerializeField] private Transform contentParent;

    [Header("버튼")]
    [SerializeField] private Button clearAllButton;
    [SerializeField] private Button backButton;

    [Header("빈 상태 텍스트")]
    [SerializeField] private TextMeshProUGUI emptyText;

    private static readonly string[] StageLabels = {
        "1스테이지 — 산속·한자어",
        "2스테이지 — 서당·속담",
        "3스테이지 — 저잣거리·외래어",
        "4스테이지 — 궁궐·사자성어",
        "5스테이지 — 저승문·혼합"
    };

    private TMP_FontAsset _font;

    private void Start()
    {
        var sample = GameObject.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
        if (sample != null) _font = sample.font;

        AutoFind();

        backButton?.onClick.AddListener(() => SceneFader.LoadScene("MainMenuScene"));
        clearAllButton?.onClick.AddListener(OnClearAll);

        Refresh();
    }

    private void Refresh()
    {
        if (contentParent == null) return;

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        var all = WrongAnswerStorage.LoadAll();
        if (emptyText != null) emptyText.gameObject.SetActive(all.Count == 0);

        for (int s = 1; s <= 5; s++)
        {
            var entries = WrongAnswerStorage.LoadForStage(s);
            if (entries.Count == 0) continue;

            AddHeader(StageLabels[s - 1]);
            foreach (var e in entries)
                AddEntry(e);
        }
    }

    private void AddHeader(string label)
    {
        var go = new GameObject("Header");
        go.transform.SetParent(contentParent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 44f);
        var le = go.AddComponent<LayoutElement>();
        le.minWidth      = 100f;
        le.flexibleWidth = 1f;
        le.preferredHeight = 44f;
        var txt = go.AddComponent<TextMeshProUGUI>();
        if (_font != null) txt.font = _font;
        txt.text = label;
        txt.fontSize = 22;
        txt.fontStyle = FontStyles.Bold;
        txt.color = new Color(1f, 0.85f, 0.3f, 1f);
        txt.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private void AddEntry(WrongAnswerEntry e)
    {
        string word     = !string.IsNullOrEmpty(e.originWord)      ? StripUnsupported(e.originWord)      : "";
        string sentence = !string.IsNullOrEmpty(e.exampleSentence) ? StripUnsupported(e.exampleSentence) : "(예문 없음)";
        string answer   = !string.IsNullOrEmpty(e.correctAnswer)   ? StripUnsupported(e.correctAnswer)   : "";

        // ── Entry: VerticalLayoutGroup ─────────────────────────────
        var go = new GameObject("Entry");
        go.transform.SetParent(contentParent, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.minWidth      = 100f;
        le.flexibleWidth = 1f;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.1f, 0.06f, 1f);
        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 10, 10);
        vlg.spacing = 6f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = true;
        var csf = go.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── 윗줄: 예문 ────────────────────────────────────────────
        var sentGO = new GameObject("SentenceText");
        sentGO.transform.SetParent(go.transform, false);
        sentGO.AddComponent<RectTransform>();
        var sTxt = sentGO.AddComponent<TextMeshProUGUI>();
        if (_font != null) sTxt.font = _font;
        sTxt.text = $"\"{sentence}\"";
        sTxt.fontSize = 19;
        sTxt.color = new Color(0.95f, 0.92f, 0.82f, 1f);
        sTxt.alignment = TextAlignmentOptions.MidlineLeft;
        sTxt.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // ── 아랫줄: HorizontalLayoutGroup (정답 + 확인 버튼) ──────
        var bottomGO = new GameObject("BottomRow");
        bottomGO.transform.SetParent(go.transform, false);
        bottomGO.AddComponent<RectTransform>();
        var hlg = bottomGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth  = true;
        hlg.childControlHeight = true;
        var bcsf = bottomGO.AddComponent<ContentSizeFitter>();
        bcsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 정답 텍스트
        var ansGO = new GameObject("AnswerText");
        ansGO.transform.SetParent(bottomGO.transform, false);
        ansGO.AddComponent<RectTransform>();
        var ansLE = ansGO.AddComponent<LayoutElement>();
        ansLE.flexibleWidth = 1f;
        var aTxt = ansGO.AddComponent<TextMeshProUGUI>();
        if (_font != null) aTxt.font = _font;
        aTxt.text = string.IsNullOrEmpty(word)
            ? $"<color=#4f4>정답: {answer}</color>"
            : $"<color=#aaa>「{word}」의 정답:</color> <color=#4f4>{answer}</color>";
        aTxt.fontSize = 17;
        aTxt.color = Color.white;
        aTxt.alignment = TextAlignmentOptions.MidlineLeft;

        // 확인 버튼
        var btnGO = new GameObject("ConfirmButton");
        btnGO.transform.SetParent(bottomGO.transform, false);
        btnGO.AddComponent<RectTransform>();
        var btnLE = btnGO.AddComponent<LayoutElement>();
        btnLE.preferredWidth  = 80f;
        btnLE.preferredHeight = 36f;
        var bImg = btnGO.AddComponent<Image>();
        bImg.color = new Color(0.2f, 0.45f, 0.25f, 1f);
        var btn = btnGO.AddComponent<Button>();

        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(btnGO.transform, false);
        var lrt = lblGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var lTxt = lblGO.AddComponent<TextMeshProUGUI>();
        if (_font != null) lTxt.font = _font;
        lTxt.text = "확인";
        lTxt.fontSize = 17;
        lTxt.alignment = TextAlignmentOptions.Center;
        lTxt.color = Color.white;

        string entryId = e.id;
        int stageIdx   = e.stageIndex;
        btn.onClick.AddListener(() =>
        {
            WrongAnswerStorage.DeleteEntry(entryId, stageIdx);
            Refresh();
        });
    }

    private void OnClearAll()
    {
        WrongAnswerStorage.ClearAll();
        Refresh();
    }

    private void AutoFind()
    {
        if (contentParent  == null) contentParent  = GameObject.Find("Content")?.transform;
        if (clearAllButton == null) clearAllButton = FindBtn("BtnClearAll");
        if (backButton     == null) backButton     = FindBtn("BackButton");
        if (emptyText      == null) emptyText      = GameObject.Find("EmptyText")?.GetComponent<TextMeshProUGUI>();
    }

    private static Button FindBtn(string name) =>
        GameObject.Find(name)?.GetComponent<Button>();

    // 네오 둥근모가 지원하지 않는 CJK 한자 제거
    private static string StripUnsupported(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var sb = new System.Text.StringBuilder();
        foreach (char c in text)
        {
            if (c >= 0x4E00 && c <= 0x9FFF) continue; // CJK 통합 한자
            if (c >= 0x3400 && c <= 0x4DBF) continue; // CJK 확장 A
            if (c >= 0xF900 && c <= 0xFAFF) continue; // CJK 호환 한자
            sb.Append(c);
        }
        return sb.ToString();
    }
}

