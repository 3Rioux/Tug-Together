// C#
using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour
{
    [field: Header("UI SFX")]
    [field: SerializeField] public EventReference UIClick { get; private set; }
    [field: SerializeField] public EventReference UIBack { get; private set; }
    
    
    [field: Header("AMBIENCE")]
    [field: SerializeField] public EventReference MainMenu { get; private set; }

    public static FMODEvents Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            //Debug.LogError("Multiple instances of FMODEvents detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}