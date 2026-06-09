using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class TitleSceneManager : MonoBehaviour
{
    private bool _inputEnabled = false;

    private void Start()
    {
        Invoke(nameof(EnableInput), 0.5f);
    }

    private void EnableInput() => _inputEnabled = true;

    private void Update()
    {
        if (!_inputEnabled) return;
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            GoToMainMenu();
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            GoToMainMenu();
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            GoToMainMenu();
    }

    public void OnStartButtonClicked() => GoToMainMenu();

    private void GoToMainMenu()
    {
        SceneFader.LoadScene("MainMenuScene");
    }
}

