using UnityEngine;
using UnityEngine.InputSystem;

public class PauseController : MonoBehaviour
{
    [SerializeField] private GameObject           pausePanelRoot; // holds LiquidBubblePanelAnimation
    [SerializeField] private InputActionReference pauseAction;    // UI / Pause

    private LiquidBubblePanelAnimation _anim;
    private bool _isPaused;

    void Awake()
    {
        _anim = pausePanelRoot.GetComponent<LiquidBubblePanelAnimation>();
        if (_anim == null) Debug.LogError("Pause panel missing LiquidBubblePanelAnimation", this);
    }

    void OnEnable()
    {
        pauseAction.action.performed += OnPausePerformed;
        pauseAction.action.Enable();
    }

    void OnDisable()
    {
        pauseAction.action.performed -= OnPausePerformed;
        pauseAction.action.Disable();
    }

    void OnPausePerformed(InputAction.CallbackContext _) => TogglePause();

    void TogglePause()
    {
        if (_anim.IsAnimating) return;            // ignore spam while animating

        if (_isPaused) ResumeGame();
        else           PauseGame();
    }

    void PauseGame()
    {
        _anim.PlayOpenAnimation();
        _isPaused = true;
    }

    void ResumeGame()
    {
        _anim.PlayCloseAnimation();
        _isPaused = false;
    }
}