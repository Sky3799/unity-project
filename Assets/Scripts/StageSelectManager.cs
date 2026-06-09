using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageSelectManager : MonoBehaviour
{
    public static readonly string[] StageNames  = { "1스테이지", "2스테이지", "3스테이지", "4스테이지", "5스테이지" };
    private static readonly string[] StageThemes = { "산속 · 공문서 한자어", "서당 · 순우리말 시간어", "저잣거리 · 생활 어휘", "궁궐 · 사회·경제 어휘", "저승문 · 혼합" };

    public static string UnlockKey(int index) => $"Stage{index + 1}_Unlocked";
    public static string DiffClearKey(int stageIndex, int diff) => $"Stage{stageIndex + 1}_Diff{diff}_Cleared";

    private void Start()
    {
        // 1스테이지만 기본 해금
        PlayerPrefs.SetInt(UnlockKey(0), 1);
        for (int i = 1; i < 5; i++)
            if (!PlayerPrefs.HasKey(UnlockKey(i))) PlayerPrefs.SetInt(UnlockKey(i), 0);
        PlayerPrefs.Save();

        for (int i = 0; i < 5; i++)
            RefreshStageButton(i);

        var backBtn = GameObject.Find("BackButton");
        backBtn?.GetComponent<Button>()?.onClick.AddListener(OnBackClicked);
    }

    private void RefreshStageButton(int i)
    {
        var btnGO = GameObject.Find("StageButton_" + (i + 1));
        if (btnGO == null) return;

        bool unlocked = PlayerPrefs.GetInt(UnlockKey(i), 0) == 1;

        var btn = btnGO.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = unlocked;
            btn.onClick.RemoveAllListeners();
            if (unlocked)
            {
                int idx = i;
                // 직접 씬 로드 대신 난이도 팝업 표시
                btn.onClick.AddListener(() => ShowDifficultyPopup(idx));
            }
        }

        var lockIcon = btnGO.transform.Find("LockIcon");
        if (lockIcon != null) lockIcon.gameObject.SetActive(!unlocked);

        var nameText  = btnGO.transform.Find("StageName")?.GetComponent<TextMeshProUGUI>();
        var themeText = btnGO.transform.Find("StageTheme")?.GetComponent<TextMeshProUGUI>();
        if (nameText  != null) nameText.text  = StageNames[i];
        if (themeText != null) themeText.text = StageThemes[i];
    }

    private void ShowDifficultyPopup(int stageIndex)
    {
        var popup = DifficultyPopup.Instance
                 ?? FindFirstObjectByType<DifficultyPopup>(FindObjectsInactive.Include);
        if (popup != null)
            popup.Show(stageIndex, StageNames[stageIndex]);
        else
            Debug.LogError("[StageSelect] DifficultyPopup을 찾을 수 없습니다.");
    }

    // 난이도 클리어 후 BattleManager에서 호출
    public static void OnDifficultyCleared(int stageIndex, int difficulty)
    {
        PlayerPrefs.SetInt(DiffClearKey(stageIndex, difficulty), 1);

        // 난이도 1/2/3 모두 클리어 → 다음 스테이지 해금
        if (AllDifficultiesCleared(stageIndex))
            UnlockNextStage(stageIndex);

        PlayerPrefs.Save();
    }

    private static bool AllDifficultiesCleared(int stageIndex)
    {
        for (int d = 1; d <= 3; d++)
            if (PlayerPrefs.GetInt(DiffClearKey(stageIndex, d), 0) == 0) return false;
        return true;
    }

    public static void UnlockNextStage(int clearedStageIndex)
    {
        int next = clearedStageIndex + 1;
        if (next < 5)
        {
            PlayerPrefs.SetInt(UnlockKey(next), 1);
            PlayerPrefs.Save();
        }
    }

    public void OnBackClicked()
    {
        SceneFader.LoadScene("MainMenuScene");
    }
}
