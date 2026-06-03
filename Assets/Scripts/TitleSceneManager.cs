using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleSceneManager : MonoBehaviour
{
    public void OnStartButtonClicked()
    {
        SceneManager.LoadScene("BattleScene");
    }
}
