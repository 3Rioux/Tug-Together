using UnityEngine;
using UnityEngine.UI;


// THIS CLASS WILL BE MERGED WITH MENU MANAGER IN THE FUTURE!
public class CreditsButtonCaller : MonoBehaviour
{
    [SerializeField] private Button creditsButton;

    void Awake()
    {
        creditsButton.onClick.RemoveAllListeners();
        creditsButton.onClick.AddListener(() =>
        {
            if (CreditsController.Instance != null)
                CreditsController.Instance.ShowCredits();
            else
                Debug.LogError("CreditsController instance not found");
        });
    }
}