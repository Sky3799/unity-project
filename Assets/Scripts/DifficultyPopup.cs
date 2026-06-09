using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DifficultyPopup : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private TextMeshProUGUI stageNameText;
    [SerializeField] private Button[] difficultyButtons;   // 0=1단계, 1=2단계, 2=3단계
    [SerializeField] private TextMeshProUGUI[] diffLabels; // 각 버튼 텍스트
    [SerializeField] private Button closeButton;

    private int _selectedStageIndex; // 0-based

    public static DifficultyPopup Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        closeButton?.onClick.AddListener(Hide);
        gameObject.SetActive(false);
    }

    public void Show(int stageIndex, string stageName)
    {
        _selectedStageIndex = stageIndex;
        stageNameText.text  = stageName;
        gameObject.SetActive(true);

        string[] labels = { "1단계  ★", "2단계  ★★", "3단계  ★★★" };

        for (int d = 0; d < 3; d++)
        {
            bool unlocked = IsDifficultyUnlocked(stageIndex, d + 1);
            int diff = d + 1;

            var btn   = difficultyButtons[d];
            var label = diffLabels[d];

            btn.interactable = unlocked;
            label.text  = unlocked ? labels[d] : $"🔒 {labels[d]}";
            label.color = unlocked
                ? new Color(1f, 0.92f, 0.6f, 1f)
                : new Color(0.5f, 0.5f, 0.5f, 1f);

            btn.onClick.RemoveAllListeners();
            if (unlocked)
                btn.onClick.AddListener(() => LoadDifficulty(diff));
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void LoadDifficulty(int difficulty)
    {
        PlayerPrefs.SetInt("CurrentStage",      _selectedStageIndex + 1);
        PlayerPrefs.SetInt("CurrentDifficulty", difficulty);
        PlayerPrefs.Save();
        Hide();
        SceneFader.LoadScene("BattleScene");
    }

    // 해금 여부: 1단계는 스테이지 자체가 해금돼 있으면 OK
    public static bool IsDifficultyUnlocked(int stageIndex, int difficulty)
    {
        string stageKey = StageSelectManager.UnlockKey(stageIndex);
        if (PlayerPrefs.GetInt(stageKey, 0) != 1) return false;

        if (difficulty == 1) return true;

        // 2단계: 1단계 클리어 여부
        // 3단계: 2단계 클리어 여부
        string prevClearKey = $"Stage{stageIndex + 1}_Diff{difficulty - 1}_Cleared";
        return PlayerPrefs.GetInt(prevClearKey, 0) == 1;
    }
}
