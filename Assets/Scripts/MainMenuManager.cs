using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("플레이 방법 팝업")]
    [SerializeField] private GameObject howToPlayPopup;

    private void Start()
    {
        // Inspector 미연결 시 transform 자식에서 탐색 (비활성 포함)
        if (howToPlayPopup == null)
        {
            var t = transform.Find("HowToPlayPopup");
            if (t != null) howToPlayPopup = t.gameObject;
        }

        // 팝업을 먼저 활성화해서 CloseButton을 Find 가능하게 만든 뒤 숨김
        if (howToPlayPopup != null) howToPlayPopup.SetActive(true);

        var closeBtn = GameObject.Find("HowToPlayPopup/Panel/CloseButton");
        closeBtn?.GetComponent<Button>()?.onClick.AddListener(OnCloseHowToPlayClicked);

        if (howToPlayPopup != null) howToPlayPopup.SetActive(false);

        // 버튼 자동 연결
        WireButton("BtnStageSelect", OnStageSelectClicked);
        WireButton("BtnCollection",  OnCollectionClicked);
        WireButton("BtnWrongNote",   OnWrongNoteClicked);
        WireButton("BtnHowToPlay",   OnHowToPlayClicked);
    }

    private static void WireButton(string name, UnityEngine.Events.UnityAction action)
    {
        var go = GameObject.Find(name);
        go?.GetComponent<Button>()?.onClick.AddListener(action);
    }

    // ─── 버튼 핸들러 ────────────────────────────────────────────────

    public void OnStageSelectClicked() => SceneFader.LoadScene("StageSelectScene");
    public void OnCollectionClicked()  => SceneFader.LoadScene("CollectionScene");
    public void OnWrongNoteClicked()   => SceneFader.LoadScene("WrongNoteScene");

    public void OnHowToPlayClicked()
    {
        if (howToPlayPopup != null) howToPlayPopup.SetActive(true);
    }

    public void OnCloseHowToPlayClicked()
    {
        if (howToPlayPopup != null) howToPlayPopup.SetActive(false);
    }
}

