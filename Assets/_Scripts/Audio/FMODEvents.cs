using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour
{
    [field: Header("UI")]
    [field: SerializeField] public EventReference UIClick { get; private set; }
    [field: SerializeField] public EventReference UIBack { get; private set; }
    [field: SerializeField] public EventReference UIScroll { get; private set; }
    
    [field: Header("SFX")]
    
    [field: Header("Player")]
    [field: SerializeField] public EventReference PlayerIdle { get; private set; }
    [field: SerializeField] public EventReference PlayerMove { get; private set; }
    [field: SerializeField] public EventReference PlayerTurning { get; private set; }
    [field: SerializeField] public EventReference PlayerHorn { get; private set; }
    
    [field: Header("Hook")]
    [field: SerializeField] public EventReference HookShoot { get; private set; }
    [field: SerializeField] public EventReference HookAttach { get; private set; }
    [field: SerializeField] public EventReference HookIdle { get; private set; }
    [field: SerializeField] public EventReference HookDetach { get; private set; }
    
    [field: Header("Impact")]
    [field: SerializeField] public EventReference PlayerImpact { get; private set; }

    
    
    [field: Header("Ambience")]
    [field: SerializeField] public EventReference MainMenu { get; private set; }
    [field: SerializeField] public EventReference Tutorial { get; private set; }
    
    [field: Header("Music")]
    [field: SerializeField] public EventReference Credits { get; private set; }

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