using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EndGameTrigger : NetworkBehaviour
{
    [Tooltip("Assign the Game Over Canvas that should be shown on all clients.")]
    [SerializeField] private GameObject gameOverCanvasPrefab; // optional if each client has it already
    [SerializeField] private TextMeshProUGUI timePlayedText;

    [SerializeField] private Button continueButton;
    [SerializeField] private CinemachineCamera endGameCamera; // stores the end game camera
    [SerializeField] private CinemachineSequencerCamera endGameSequenceCamera; // stores the end game Sequencer camera

    [SerializeField]
    private List<GameObject> disableObjects = new List<GameObject>();


    [SerializeField] private MatchTimer timer;

    private bool buttonPressed = false;



    private void Start()
    {
        gameOverCanvasPrefab.SetActive(false); // off by default
        endGameSequenceCamera.gameObject.SetActive(false);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TriggerGameOverClientRpc();
        }
#endif
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // Only the server should handle the trigger

        if (other.CompareTag("Barge"))
        {
            Debug.Log("Barge has entered the end zone!");
            TriggerGameOverClientRpc();
        }
    }

    [ClientRpc]
    void TriggerGameOverClientRpc()
    {
        Debug.Log("Triggering Game Over UI on client");

        Cursor.lockState = CursorLockMode.Confined;

        // Try to find a canvas tagged or named appropriately, or make this reference explicit
        //GameObject canvas = GameObject.Find("GameOverCanvas"); // or use a static reference/UI manager
       
        if (gameOverCanvasPrefab != null)
        {
            StartCoroutine(ShowGameOverSequence());
            //gameOverCanvasPrefab.SetActive(true);

            //hide all wanted objects 
            foreach (GameObject go in disableObjects)
            {
                go.SetActive(false);
            }


            //stop timer 
            timer.EndMatch();
            if (timePlayedText != null) timePlayedText.text = $"Time Played: {timer.TotalTimePlayed}";

            //CreditsController.Instance.ShowCredits();
        }
        else
        {
            Debug.LogWarning("GameOverCanvas not found in the scene.");
        }
    }


    private IEnumerator ShowGameOverSequence()
    {
        endGameSequenceCamera.gameObject.SetActive(true);
       // gameOverCanvasPrefab.SetActive(true);


        // Hook up the button
        continueButton.onClick.AddListener(OnContinueButtonPressed);


        float startTime = Time.time;
        float timeout = 2f;


        // Wait until the button is pressed OR 5 seconds have passed 
        yield return new WaitUntil(() => buttonPressed || Time.time - startTime >= timeout);


        // Clean up listener
        continueButton.onClick.RemoveListener(OnContinueButtonPressed);


        //yield return new WaitForSeconds(2f);

        gameOverCanvasPrefab.SetActive(false);

        CreditsController.Instance.ShowCredits();
    }

    private void OnDisable()
    {
        // Clean up listener just in case
        continueButton.onClick.RemoveListener(OnContinueButtonPressed);
    }

    private void OnContinueButtonPressed()
    {
        buttonPressed = true;
    }


}
