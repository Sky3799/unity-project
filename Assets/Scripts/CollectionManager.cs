using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CollectionManager : MonoBehaviour
{
    private static readonly string[] StageNames = {
        "1스테이지\n산속·한자어",
        "2스테이지\n서당·순우리말",
        "3스테이지\n저잣거리·생활어휘",
        "4스테이지\n궁궐·사회경제어휘",
        "5스테이지\n저승문·혼합"
    };

    private const float CardW = 340f;
    private const float CardH = 460f;

    [SerializeField] private Transform contentParent;
    [SerializeField] private Button backButton;

    private TMP_FontAsset _font;

    private void Start()
    {
        var sample = GameObject.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
        if (sample != null) _font = sample.font;
        if (contentParent == null) contentParent = GameObject.Find("Content")?.transform;
        if (backButton    == null) backButton    = GameObject.Find("BackButton")?.GetComponent<Button>();

        backButton?.onClick.AddListener(() => SceneFader.LoadScene("MainMenuScene"));
        BuildCards();
    }

    private void BuildCards()
    {
        if (contentParent == null) return;
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        for (int s = 1; s <= 5; s++)
        {
            bool   cleared = CollectionStorage.IsCleared(s);
            string monster = CollectionStorage.GetMonster(s);
            string title   = CollectionStorage.GetTitle(s);
            Sprite sprite  = cleared ? CollectionStorage.GetMonsterSprite(s) : null;

            // ── 카드 루트 ──────────────────────────────────────────
            var card = new GameObject($"Card_Stage{s}");
            card.transform.SetParent(contentParent, false);
            var rt = card.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(CardW, CardH);
            var le = card.AddComponent<LayoutElement>();
            le.preferredWidth  = CardW;
            le.preferredHeight = CardH;
            var bg = card.AddComponent<Image>();
            bg.color = cleared
                ? new Color(0.15f, 0.11f, 0.05f, 1f)
                : new Color(0.07f, 0.07f, 0.07f, 0.92f);

            // ── 스테이지 이름 (상단) ───────────────────────────────
            AddText(card.transform, "StageName", StageNames[s - 1], 17,
                new Vector2(0f, CardH * 0.5f - 48f), CardW - 20f, 52f,
                new Color(1f, 0.85f, 0.3f, 1f));

            // ── 몬스터 이미지 or 텍스트 박스 (중앙) ──────────────
            var monBoxGO = new GameObject("MonsterBox");
            monBoxGO.transform.SetParent(card.transform, false);
            var mrt = monBoxGO.AddComponent<RectTransform>();
            mrt.anchoredPosition = new Vector2(0f, 10f);
            mrt.sizeDelta = new Vector2(CardW - 24f, CardH * 0.56f);

            if (sprite != null)
            {
                // 실제 이미지 표시
                var mImg = monBoxGO.AddComponent<Image>();
                mImg.sprite = sprite;
                mImg.preserveAspect = true;
                mImg.color = Color.white;
            }
            else
            {
                // 이미지 없으면 배경 + 텍스트
                var mImg = monBoxGO.AddComponent<Image>();
                mImg.color = cleared
                    ? new Color(0.1f, 0.08f, 0.04f, 1f)
                    : new Color(0.04f, 0.04f, 0.04f, 0.85f);

                AddText(monBoxGO.transform, "MonsterLabel",
                    cleared ? monster : "?",
                    cleared ? 32 : 48,
                    Vector2.zero, CardW - 40f, CardH * 0.5f,
                    cleared ? Color.white : new Color(0.35f, 0.35f, 0.35f));
            }

            // ── 몬스터 이름 (이미지 아래) ─────────────────────────
            if (cleared)
            {
                AddText(card.transform, "MonsterName", monster, 20,
                    new Vector2(0f, -(CardH * 0.5f - 90f)), CardW - 20f, 34f,
                    Color.white);
            }

            // ── 칭호 (하단) ────────────────────────────────────────
            AddText(card.transform, "TitleLabel",
                cleared ? $"칭호  {title}" : "???", 16,
                new Vector2(0f, -(CardH * 0.5f - 48f)), CardW - 20f, 40f,
                cleared ? new Color(0.8f, 0.7f, 1f, 1f) : new Color(0.3f, 0.3f, 0.3f));
        }
    }

    private void AddText(Transform parent, string name, string text, int size,
        Vector2 pos, float w, float h, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(w, h);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (_font != null) tmp.font = _font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
    }
}

