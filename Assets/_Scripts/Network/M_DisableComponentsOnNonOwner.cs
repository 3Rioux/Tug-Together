using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Enables/disables components based on being the Owner NetworkBehaviour 
/// </summary>
public class M_DisableComponentsOnNonOwner : NetworkBehaviour
{
    [SerializeField] private GameObject m_Cameras; // stores the cameras parent 
    [SerializeField] private TextMeshPro m_NameTagText; // stores the name tag world textbox 

    private ulong m_ThisPlayerID;

    private void Start()
    {
        if (IsOwner && SceneManager.GetActiveScene().name != "MainMenu")
        {
            m_ThisPlayerID = NetworkManager.Singleton.LocalClientId; //OwnerClientId

            //set cameras to active if the owner 
            if (m_Cameras == null) transform.Find("PlayerCameras");
            m_Cameras.SetActive(true);

            
        }
        else
        {
            //IF NOT THE OWNER 
            if (m_Cameras == null) transform.Find("PlayerCameras");
            m_Cameras.SetActive(false);

            // only display name tags for the other players (dont need one for the owner, just gets in the way) 
            if (m_NameTagText == null) transform.Find("PlayerNameTag");
            m_NameTagText.text = m_ThisPlayerID.ToString(); // Temp Use the network ID instead of username 
        }


    }//end start 


    public void EnableCameras()
    {
        if (IsOwner)
        {
            //IF NOT THE OWNER 
            if (m_Cameras == null) transform.Find("PlayerCameras");
            m_Cameras.SetActive(true);
        }
    }













}
