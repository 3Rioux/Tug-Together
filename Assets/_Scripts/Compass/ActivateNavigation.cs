using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// This script will activate /Deactivate the Navigation components (Compass, Map...)
/// 
/// toggle Dest NOT work rn 
/// </summary>
public class ActivateNavigation : NetworkBehaviour
{
    [SerializeField] private Transform navigationParent;
    [SerializeField] private CinemachineCamera navCamera;

     private BoatInputActions controls;
    private bool isMapActive = false;

    [SerializeField] private bool isToggle = true;

    private void Awake()
    {
        navigationParent.gameObject.SetActive(false);
        navCamera.gameObject.SetActive(false); // always off by default

        controls = new BoatInputActions();

        if (isToggle)
        {//Active till button pressed again 
            //Map Input
            controls.Boat.ToggleMap.performed += ctx => ToggleNavigation();
            //controls.Boat.ToggleMap.canceled += _ => ToggleNavigation();
        }
        else
        {//Only active while tab is pressed
            //Map Input
            controls.Boat.ToggleMap.performed += ctx => HoldNavigation(true);
            controls.Boat.ToggleMap.canceled += _ => HoldNavigation(false);
        }
    }

    void OnEnable()
    {
        controls.Enable();
    }

    void OnDisable()
    {
        controls.Disable();
    }


    /// <summary>
    /// Toggles the navigation Enabled Disabled 
    /// </summary>
    private void ToggleNavigation()
    {
        if (!IsOwner) return;
        //Debug.Log("Trigger Toggle Navigation");
        isMapActive = !isMapActive;

        navigationParent.gameObject.SetActive(isMapActive);
        navCamera.gameObject.SetActive(isMapActive);
    }

    /// <summary>
    /// Activate/Deactivate the Navigation based on given bool 
    /// </summary>
    /// <param name="trigger"></param>
    private void HoldNavigation(bool trigger)
    {
        if (!IsOwner) return;
        navigationParent.gameObject.SetActive(trigger);
        navCamera.gameObject.SetActive(trigger);
    }


}
