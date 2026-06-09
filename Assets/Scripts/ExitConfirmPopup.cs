using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ExitConfirmPopup : MonoBehaviour
{
    [SerializeField] private GameObject popup;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private void Start()
    {
        // transform.Find는 비활성 자식도 탐색 가능
        if (popup     == null) popup      = transform.Find("ExitPopup")?.gameObject;
        if (exitButton == null) exitButton = transform.Find("BtnExit")?.GetComponent<Button>();

        // 팝업 내부 버튼은 잠깐 활성화 후 탐색
        if (popup != null)
        {
            popup.SetActive(true);
            if (yesButton == null) yesButton = popup.transform.Find("Panel/BtnYes")?.GetComponent<Button>();
            if (noButton  == null) noButton  = popup.transform.Find("Panel/BtnNo")?.GetComponent<Button>();
            popup.SetActive(false);
        }

        exitButton?.onClick.AddListener(OnExitClicked);
        yesButton?.onClick.AddListener(OnYesClicked);
        noButton?.onClick.AddListener(OnNoClicked);
    }

    private void OnExitClicked()
    {
        if (popup != null) popup.SetActive(true);
    }

    private void OnYesClicked()
    {
        SceneFader.LoadScene("StageSelectScene");
    }

    private void OnNoClicked()
    {
        if (popup != null) popup.SetActive(false);
    }
}

