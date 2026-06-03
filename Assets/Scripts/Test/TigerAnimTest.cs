using UnityEngine;
using UnityEngine.InputSystem;

public class TigerAnimTest : MonoBehaviour
{
    [SerializeField] private Animator tigerAnimator;

    private void Update()
    {
        if (Keyboard.current[Key.Digit2].wasPressedThisFrame)
            tigerAnimator?.SetTrigger("Attack");

        if (Keyboard.current[Key.Digit3].wasPressedThisFrame)
            tigerAnimator?.SetTrigger("Hit");
    }
}
