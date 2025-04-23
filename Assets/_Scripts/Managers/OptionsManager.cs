// C#
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class OptionsManager : MonoBehaviour
{
    // [Header("Panels")]
    // [SerializeField] private GameObject settingsMenu;
    // [SerializeField] private GameObject videoMenu;
    // [SerializeField] private GameObject audioMenu;
    //
    // [Header("Input")]
    // [SerializeField] private InputActionReference backActionReference;
    //
    // private void Awake()
    // {
    //     if (backActionReference != null)
    //     {
    //         backActionReference.action.performed += OnBackActionPerformed;
    //     }
    //     else
    //     {
    //         Debug.LogError("Back action reference not assigned in the inspector.");
    //     }
    // }
    //
    // private void OnEnable()
    // {
    //     backActionReference?.action.Enable();
    //
    //     // Ensure initial panel state
    //     if (settingsMenu != null)
    //     {
    //         settingsMenu.SetActive(true);
    //     }
    //     if (videoMenu != null)
    //     {
    //         videoMenu.SetActive(false);
    //     }
    //     if (audioMenu != null)
    //     {
    //         audioMenu.SetActive(false);
    //     }
    // }
    //
    // private void OnDisable()
    // {
    //     if (backActionReference != null)
    //     {
    //         backActionReference.action.performed -= OnBackActionPerformed;
    //         backActionReference.action.Disable();
    //     }
    // }
    //
    // private void OnBackActionPerformed(InputAction.CallbackContext context)
    // {
    //     OnBackButtonClick();
    // }
    //
    // private void Start()
    // {
    //     if (settingsMenu == null || videoMenu == null || audioMenu == null)
    //     {
    //         Debug.LogError("One or more menu panels are not assigned in the inspector.");
    //         return;
    //     }
    //     // Set initial states in case OnEnable wasn't sufficient
    //     settingsMenu.SetActive(true);
    //     videoMenu.SetActive(false);
    //     audioMenu.SetActive(false);
    // }
    //
    // public void OnVideoButtonClick()
    // {
    //     settingsMenu.SetActive(false);
    //     AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
    //     videoMenu.SetActive(true);
    // }
    //
    // public void OnAudioButtonClick()
    // {
    //     settingsMenu.SetActive(false);
    //     AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
    //     audioMenu.SetActive(true);
    // }
    //
    // public void OnBackButtonClick()
    // {
    //     AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIBack, transform.position);
    //     if (settingsMenu != null) settingsMenu.SetActive(true);
    //     if (videoMenu != null) videoMenu.SetActive(false);
    //     if (audioMenu != null) audioMenu.SetActive(false);
    // }
}