using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class NotificationPopup : MonoBehaviour
{
    [SerializeField] private GameObject uiNotificationPopup;//store the exclamation mark GObj in local scene 
    [SerializeField] private UIPulseBreath uiNotificationPopupEffect;

    private BoatInputActions controls;

    private void Awake()
    {
        controls = new BoatInputActions();
        controls.Boat.ToggleMap.performed += ctx => HideNotification();
    }

    void OnEnable()
    {
        controls.Enable();
        LevelProgressionManager.OnMissionGoalUpdated += EnablePopup;
    }

    void OnDisable()
    {
        controls.Disable();
        LevelProgressionManager.OnMissionGoalUpdated -= EnablePopup;
    }

    private void HideNotification()
    {
        uiNotificationPopup.SetActive(false);
    }

    private void EnablePopup(string goal)
    {
        StartCoroutine("LetLocalPlayerKnowGoalChanged");
    }

    private IEnumerable LetLocalPlayerKnowGoalChanged()
    {
        Debug.Log("LetLocalPlayerKnowGoalChanged");
        uiNotificationPopup.SetActive(true);
        uiNotificationPopupEffect.enabled = true;

        //float timer = 0f;
        //float waitTime = 10f;
        yield return new WaitForSeconds(10f);

        //while (timer < waitTime && !Keyboard.current.tabKey.wasPressedThisFrame)
        //{
        //    timer += Time.deltaTime;
        //    yield return null;
        //}

        Debug.Log("Exit LetLocalPlayerKnowGoalChanged");

        uiNotificationPopupEffect.enabled = false;
    }
}
