using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ResultSceneManager : MonoBehaviour
{
    [Header("결과 텍스트")]
    [SerializeField] private TextMeshProUGUI resultTitleText;   // 클리어 / 실패
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI correctText;
    [SerializeField] private TextMeshProUGUI wrongText;
    [SerializeField] private TextMeshProUGUI accuracyText;

    [Header("클리어 전용")]
    [SerializeField] private GameObject clearOnlyGroup;         // 칭호+몬스터 그룹
    [SerializeField] private TextMeshProUGUI monsterText;
    [SerializeField] private TextMeshProUGUI titleAwardText;

    [Header("버튼")]
    [SerializeField] private Button reviewButton;
    [SerializeField] private Button mainMenuButton;

    private void Start()
    {
        // Inspector 미연결 시 이름으로 자동 탐색
        AutoFind();

        var d = BattleResultData.IsCleared;
        int ts = (int)BattleResultData.ElapsedTime;

        if (resultTitleText != null)
        {
            resultTitleText.text  = d ? "CLEAR!" : "FAILED";
            resultTitleText.color = d ? new Color(1f, 0.85f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);
        }

        if (timeText     != null) timeText.text     = $"소요 시간\n<indent=12px>{ts / 60:D2}:{ts % 60:D2}</indent>";
        if (correctText  != null) correctText.text  = $"맞춘 문제\n<indent=12px>{BattleResultData.CorrectCount}개</indent>";
        if (wrongText    != null) wrongText.text    = $"틀린 문제\n<indent=12px>{BattleResultData.WrongCount}개</indent>";
        if (accuracyText != null) accuracyText.text = $"정답률\n<indent=12px>{BattleResultData.AccuracyPercent:F0}%</indent>";

        if (clearOnlyGroup != null) clearOnlyGroup.SetActive(d);

        // 실패 시 StatsPanel을 중앙으로 이동
        var statsPanel = GameObject.Find("StatsPanel");
        if (statsPanel != null)
        {
            var rt = statsPanel.GetComponent<UnityEngine.RectTransform>();
            if (rt != null)
                rt.anchoredPosition = d ? new UnityEngine.Vector2(-170f, 30f) : new UnityEngine.Vector2(0f, 30f);
        }
        if (d)
        {
            int stage = BattleResultData.StageIndex;
            if (monsterText    != null) monsterText.text    = $"획득 몬스터\n<indent=12px>{BattleResultData.MonsterName}</indent>";
            if (titleAwardText != null) titleAwardText.text = $"획득 칭호\n<indent=12px>{CollectionStorage.GetTitle(stage)}</indent>";
        }

        if (reviewButton != null) reviewButton.gameObject.SetActive(false);
        mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
    }

    private void AutoFind()
    {
        if (resultTitleText == null) resultTitleText = Find<TextMeshProUGUI>("ResultTitleText");
        if (timeText        == null) timeText        = Find<TextMeshProUGUI>("TimeText");
        if (correctText     == null) correctText     = Find<TextMeshProUGUI>("CorrectText");
        if (wrongText       == null) wrongText       = Find<TextMeshProUGUI>("WrongText");
        if (accuracyText    == null) accuracyText    = Find<TextMeshProUGUI>("AccuracyText");
        if (clearOnlyGroup  == null) clearOnlyGroup  = GameObject.Find("ClearOnlyGroup");

        // MonsterText / TitleAwardText 는 ClearOnlyGroup 자식이라 활성화 후 탐색
        if (clearOnlyGroup != null) clearOnlyGroup.SetActive(true);
        if (monsterText     == null) monsterText     = Find<TextMeshProUGUI>("MonsterText");
        if (titleAwardText  == null) titleAwardText  = Find<TextMeshProUGUI>("TitleAwardText");
        if (clearOnlyGroup != null) clearOnlyGroup.SetActive(false); // 일단 숨겨두기
        if (reviewButton    == null) reviewButton    = FindBtn("BtnReview");
        if (mainMenuButton  == null) mainMenuButton  = FindBtn("BtnMainMenu");
    }

    private static T Find<T>(string name) where T : Component =>
        GameObject.Find(name)?.GetComponent<T>();
    private static Button FindBtn(string name) =>
        GameObject.Find(name)?.GetComponent<Button>();

    private void OnReviewClicked()
    {
        BattleResultData.IsReviewMode  = true;
        BattleResultData.ReviewEntries = WrongAnswerStorage.LoadForStage(BattleResultData.StageIndex);
        PlayerPrefs.SetInt("CurrentStage", BattleResultData.StageIndex);
        SceneFader.LoadScene("BattleScene");
    }

    private void OnMainMenuClicked() => SceneFader.LoadScene("MainMenuScene");
}

