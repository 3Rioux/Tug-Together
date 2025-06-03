using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/*//--------------------------------------------------------------------New
 *  How to Rebind Your Controls in Unity (With Icons!) | Input System: 
 * https://www.youtube.com/watch?v=qXbjyzBlduY
 */


////TODO: localization support

////TODO: deal with composites that have parts bound in different control schemes


/// <summary>
/// A reusable component with a self-contained UI for rebinding a single action.
/// </summary>
public class RebindActionUI : MonoBehaviour
{
    /// <summary>
    /// Reference to the action that is to be rebound.
    /// </summary>
    public InputActionReference actionReference
    {
        get => m_Action;
        set
        {
            m_Action = value;
            UpdateActionLabel();
            UpdateBindingDisplay();
        }
    }

    /// <summary>
    /// ID (in string form) of the binding that is to be rebound on the action.
    /// </summary>
    /// <seealso cref="InputBinding.id"/>
    public string bindingId
    {
        get => m_BindingId;
        set
        {
            m_BindingId = value;
            UpdateBindingDisplay();
        }
    }

    public InputBinding.DisplayStringOptions displayStringOptions
    {
        get => m_DisplayStringOptions;
        set
        {
            m_DisplayStringOptions = value;
            UpdateBindingDisplay();
        }
    }

    /// <summary>
    /// Text component that receives the name of the action. Optional.
    /// </summary>
    public TMPro.TextMeshProUGUI actionLabel
    {
        get => m_ActionLabel;
        set
        {
            m_ActionLabel = value;
            UpdateActionLabel();
        }
    }

    /// <summary>
    /// Text component that receives the display string of the binding. Can be <c>null</c> in which
    /// case the component entirely relies on <see cref="updateBindingUIEvent"/>.
    /// </summary>
    public TMPro.TextMeshProUGUI bindingText
    {
        get => m_BindingText;
        set
        {
            m_BindingText = value;
            UpdateBindingDisplay();
        }
    }

    /// <summary>
    /// Optional text component that receives a text prompt when waiting for a control to be actuated.
    /// </summary>
    /// <seealso cref="startRebindEvent"/>
    /// <seealso cref="rebindOverlay"/>
    public TMPro.TextMeshProUGUI rebindPrompt
    {
        get => m_RebindText;
        set => m_RebindText = value;
    }

    /// <summary>
    /// Optional UI that is activated when an interactive rebind is started and deactivated when the rebind
    /// is finished. This is normally used to display an overlay over the current UI while the system is
    /// waiting for a control to be actuated.
    /// </summary>
    /// <remarks>
    /// If neither <see cref="rebindPrompt"/> nor <c>rebindOverlay</c> is set, the component will temporarily
    /// replaced the <see cref="bindingText"/> (if not <c>null</c>) with <c>"Waiting..."</c>.
    /// </remarks>
    /// <seealso cref="startRebindEvent"/>
    /// <seealso cref="rebindPrompt"/>
    public GameObject rebindOverlay
    {
        get => m_RebindOverlay;
        set => m_RebindOverlay = value;
    }

    /// <summary>
    /// Event that is triggered every time the UI updates to reflect the current binding.
    /// This can be used to tie custom visualizations to bindings.
    /// </summary>
    public UpdateBindingUIEvent updateBindingUIEvent
    {
        get
        {
            if (m_UpdateBindingUIEvent == null)
                m_UpdateBindingUIEvent = new UpdateBindingUIEvent();
            return m_UpdateBindingUIEvent;
        }
    }

    /// <summary>
    /// Event that is triggered when an interactive rebind is started on the action.
    /// </summary>
    public InteractiveRebindEvent startRebindEvent
    {
        get
        {
            if (m_RebindStartEvent == null)
                m_RebindStartEvent = new InteractiveRebindEvent();
            return m_RebindStartEvent;
        }
    }

    /// <summary>
    /// Event that is triggered when an interactive rebind has been completed or canceled.
    /// </summary>
    public InteractiveRebindEvent stopRebindEvent
    {
        get
        {
            if (m_RebindStopEvent == null)
                m_RebindStopEvent = new InteractiveRebindEvent();
            return m_RebindStopEvent;
        }
    }

    /// <summary>
    /// When an interactive rebind is in progress, this is the rebind operation controller.
    /// Otherwise, it is <c>null</c>.
    /// </summary>
    public InputActionRebindingExtensions.RebindingOperation ongoingRebind => m_RebindOperation;

    /// <summary>
    /// Return the action and binding index for the binding that is targeted by the component
    /// according to
    /// </summary>
    /// <param name="action"></param>
    /// <param name="bindingIndex"></param>
    /// <returns></returns>
    public bool ResolveActionAndBinding(out InputAction action, out int bindingIndex)
    {
        bindingIndex = -1;

        action = m_Action?.action;
        if (action == null)
            return false;

        if (string.IsNullOrEmpty(m_BindingId))
            return false;

        // Look up binding index.
        var bindingId = new Guid(m_BindingId);
        bindingIndex = action.bindings.IndexOf(x => x.id == bindingId);
        if (bindingIndex == -1)
        {
            Debug.LogError($"Cannot find binding with ID '{bindingId}' on '{action}'", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Trigger a refresh of the currently displayed binding.
    /// </summary>
    public void UpdateBindingDisplay()
    {
        var displayString = string.Empty;
        var deviceLayoutName = default(string);
        var controlPath = default(string);

        // Get display string from action.
        var action = m_Action?.action;
        if (action != null)
        {
            var bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == m_BindingId);
            if (bindingIndex != -1)
                displayString = action.GetBindingDisplayString(bindingIndex, out deviceLayoutName, out controlPath, displayStringOptions);
        }

        // Set on label (if any).
        if (m_BindingText != null)
            m_BindingText.text = displayString;

        // Give listeners a chance to configure UI in response.
        m_UpdateBindingUIEvent?.Invoke(this, displayString, deviceLayoutName, controlPath);
    }

    /// <summary>
    /// Remove currently applied binding overrides.
    /// </summary>
    public void ResetToDefault()
    {
        if (!ResolveActionAndBinding(out var action, out var bindingIndex))
            return;

        //reset with SWAP for duplicates --------------------------------------------------------------------New
        ResetBinging(action, bindingIndex);
        //if (action.bindings[bindingIndex].isComposite)
        //{
        //    // It's a composite. Remove overrides from part bindings.
        //    for (var i = bindingIndex + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; ++i)
        //        action.RemoveBindingOverride(i);
        //}
        //else
        //{
        //    action.RemoveBindingOverride(bindingIndex);
        //}

        UpdateBindingDisplay();
    }//edn ResetToDefault


    /// <summary> --------------------------------------------------------------------New
    /// This will make sure the reset will if duplicate found SWAP the current binding with the duplicate binding
    /// </summary>
    /// <param name="action"></param>
    /// <param name="bindingIndex"></param>
    private void ResetBinging(InputAction action, int bindingIndex)
    {
        //cash a reference to the current binding:
        InputBinding newBinding = action.bindings[bindingIndex];
        string oldOverridePath = newBinding.overridePath;


        //remove the current binding 
        action.RemoveBindingOverride(bindingIndex);

        //check all actions in the map
        foreach (InputAction otherAction in action.actionMap.actions)
        {
            //if this is the original action 
            if (otherAction == action)
            {
                //skip it if its the Original action 
                continue;
            }

            //else we will check all binging for any duplicates 
            for (int i = 0; i < otherAction.bindings.Count; i++)
            {
                InputBinding currentBinding = otherAction.bindings[i];
                if (currentBinding.overridePath == newBinding.overridePath)
                {
                    //if duplicate found SWAP the bindings
                    otherAction.ApplyBindingOverride(i, oldOverridePath);
                }

            }

        }//end foreach 
    }//end ResetBinging

    /// <summary>
    /// Initiate an interactive rebind that lets the player actuate a control to choose a new binding
    /// for the action.
    /// </summary>
    public void StartInteractiveRebind()
    {
        if (!ResolveActionAndBinding(out var action, out var bindingIndex))
            return;

        // If the binding is a composite, we need to rebind each part in turn.
        if (action.bindings[bindingIndex].isComposite)
        {
            var firstPartIndex = bindingIndex + 1;
            if (firstPartIndex < action.bindings.Count && action.bindings[firstPartIndex].isPartOfComposite)
                PerformInteractiveRebind(action, firstPartIndex, allCompositeParts: true);
        }
        else
        {
            PerformInteractiveRebind(action, bindingIndex);
        }
    }

    private void PerformInteractiveRebind(InputAction action, int bindingIndex, bool allCompositeParts = false)
    {
        m_RebindOperation?.Cancel(); // Will null out m_RebindOperation.

        void CleanUp()
        {
            m_RebindOperation?.Dispose();
            m_RebindOperation = null;
            action.Enable();
        }

        //disable the action before use to prevent errors --------------------------------------------------------------------New
        action.Disable();


        //Fixes the "InvalidOperationException: Cannot rebind action x while it is enabled" error
        action.Disable();

        // Configure the rebind.
        m_RebindOperation = action.PerformInteractiveRebinding(bindingIndex)
            //Add a Excude button/buttons to the action (Stop player from using certain buttons  --------------------------------------------------------------------New
            .WithControlsExcluding("<mouse>")//exclude the entire mouse
            .WithControlsExcluding("<mouse>/middlebutton")//exclude the certain clicks on the mouse
            .WithControlsExcluding("<Keyboard>/escape")
            //Add a cancel button to the action --------------------------------------------------------------------New
            .WithCancelingThrough("<Keyboard>/escape")
            .OnCancel(
                operation =>
                {
                    //reenable them in Operation --------------------------------------------------------------------New
                    action.Enable();
                    m_RebindStopEvent?.Invoke(this, operation);
                    if (m_RebindOverlay != null)
                        m_RebindOverlay.SetActive(false);
                    UpdateBindingDisplay();
                    CleanUp();
                })
            .OnComplete(
                operation =>
                {
                    //reenable them in Operation --------------------------------------------------------------------New
                    action.Enable();
                    if (m_RebindOverlay != null)
                        m_RebindOverlay.SetActive(false);
                    m_RebindStopEvent?.Invoke(this, operation);

                    // Check to make sure no duplicate bindings are input before Updating the bindings --------------------------------------------------------------------New
                    if (CheckDuplicateBindings(action, bindingIndex, allCompositeParts))
                    {
                        //if we HAVE a duplicate remove the new binding overide 
                        action.RemoveBindingOverride(bindingIndex);
                        CleanUp();

                        //call the function again to get a new input:
                        PerformInteractiveRebind(action, bindingIndex);

                        //return to cancel out / go back to top and not remap the button 
                        return;
                    }




                    UpdateBindingDisplay();
                    CleanUp();

                    // If there's more composite parts we should bind, initiate a rebind
                    // for the next part.
                    if (allCompositeParts)
                    {
                        var nextBindingIndex = bindingIndex + 1;
                        if (nextBindingIndex < action.bindings.Count && action.bindings[nextBindingIndex].isPartOfComposite)
                            PerformInteractiveRebind(action, nextBindingIndex, true);
                    }
                });

        // If it's a part binding, show the name of the part in the UI.
        var partName = default(string);
        if (action.bindings[bindingIndex].isPartOfComposite)
            partName = $"Binding '{action.bindings[bindingIndex].name}'. ";

        // Bring up rebind overlay, if we have one.
        m_RebindOverlay?.SetActive(true);
        if (m_RebindText != null)
        {
            var text = !string.IsNullOrEmpty(m_RebindOperation.expectedControlType)
                ? $"{partName}Waiting for {m_RebindOperation.expectedControlType} input..."
                : $"{partName}Waiting for input...";
            m_RebindText.text = text;
        }

        // If we have no rebind overlay and no callback but we have a binding text label,
        // temporarily set the binding text label to "<Waiting>".
        if (m_RebindOverlay == null && m_RebindText == null && m_RebindStartEvent == null && m_BindingText != null)
            m_BindingText.text = "<Waiting...>";

        // Give listeners a chance to act on the rebind starting.
        m_RebindStartEvent?.Invoke(this, m_RebindOperation);

        m_RebindOperation.Start();
    }

    /// <summary> --------------------------------------------------------------------New
    /// This will stop the player from adding more than 1 of the same keybind 
    /// 
    /// ***Also need to change the ResetToDefault function or when its pressed you can get duplicates***
    /// </summary>
    /// <param name="action"></param>
    /// <param name="bindingIndex"></param>
    /// <param name="allCompositeParts"></param>
    /// <returns></returns>
    private bool CheckDuplicateBindings(InputAction action, int bindingIndex, bool allCompositeParts = false)
    {
        //get the current input binding
        InputBinding newBinding = action.bindings[bindingIndex];

        //check ALL the currently set bindings 
        foreach (InputBinding binding in action.bindings)
        {
            if (binding.action == newBinding.action)
            {
                continue;
            }

            //check if duplicate 
            if (binding.effectivePath == newBinding.effectivePath)
            {
                Debug.Log("Duplicate binding found: " + newBinding.effectivePath);
                return true;
            }
        }//end foreach 

        //Check when multiple inputs at once are duplicates or not (ex: WASD)
        if (allCompositeParts)
        {
            //if true check ALL binding BEFORE the current binding 
            for (int i = 1; i < bindingIndex; i++)
            {
                if (action.bindings[i].effectivePath == newBinding.overridePath)
                {
                    Debug.Log("Duplicate binding found: " + newBinding.effectivePath);
                    return true;
                }
            }
        }

        //if no duplicates where found 
        return false;

    }//edn CheckDuplicateBindings

    protected void OnEnable()
    {
        if (s_RebindActionUIs == null)
            s_RebindActionUIs = new List<RebindActionUI>();
        s_RebindActionUIs.Add(this);
        if (s_RebindActionUIs.Count == 1)
            InputSystem.onActionChange += OnActionChange;
    }

    protected void OnDisable()
    {
        m_RebindOperation?.Dispose();
        m_RebindOperation = null;

        s_RebindActionUIs.Remove(this);
        if (s_RebindActionUIs.Count == 0)
        {
            s_RebindActionUIs = null;
            InputSystem.onActionChange -= OnActionChange;
        }
    }

    // When the action system re-resolves bindings, we want to update our UI in response. While this will
    // also trigger from changes we made ourselves, it ensures that we react to changes made elsewhere. If
    // the user changes keyboard layout, for example, we will get a BoundControlsChanged notification and
    // will update our UI to reflect the current keyboard layout.
    private static void OnActionChange(object obj, InputActionChange change)
    {
        if (change != InputActionChange.BoundControlsChanged)
            return;

        var action = obj as InputAction;
        var actionMap = action?.actionMap ?? obj as InputActionMap;
        var actionAsset = actionMap?.asset ?? obj as InputActionAsset;

        for (var i = 0; i < s_RebindActionUIs.Count; ++i)
        {
            var component = s_RebindActionUIs[i];
            var referencedAction = component.actionReference?.action;
            if (referencedAction == null)
                continue;

            if (referencedAction == action ||
                referencedAction.actionMap == actionMap ||
                referencedAction.actionMap?.asset == actionAsset)
                component.UpdateBindingDisplay();
        }
    }

    [Tooltip("Reference to action that is to be rebound from the UI.")]
    [SerializeField]
    private InputActionReference m_Action;

    [SerializeField]
    private string m_BindingId;

    [SerializeField]
    private InputBinding.DisplayStringOptions m_DisplayStringOptions;

    [Tooltip("Text label that will receive the name of the action. Optional. Set to None to have the "
        + "rebind UI not show a label for the action.")]
    [SerializeField]
    private TMPro.TextMeshProUGUI m_ActionLabel;

    [Tooltip("Text label that will receive the current, formatted binding string.")]
    [SerializeField]
    private TMPro.TextMeshProUGUI m_BindingText;

    [Tooltip("Optional UI that will be shown while a rebind is in progress.")]
    [SerializeField]
    private GameObject m_RebindOverlay;

    //--------------------------------------------------------------------New
    //***For these to appear Need to edit the RebindActionUIEditor scrip ***
    //These are to allow you to change the names of the inputs to a custom text rather than the actions name 
    [Tooltip("Optional bool feaild which allows you to OVERRIDE the action label with your own text")]
    [SerializeField]
    public bool m_OverrideActionLable;

    [Tooltip("What text should be displayed for the actions lable if OVERRIDE is enabled")]
    [SerializeField]
    private string m_ActionsLabelOverideString;

    //--------------------------------------------------------------------New

    [Tooltip("Optional text label that will be updated with prompt for user input.")]
    [SerializeField]
    private TMPro.TextMeshProUGUI m_RebindText;

    [Tooltip("Event that is triggered when the way the binding is display should be updated. This allows displaying "
        + "bindings in custom ways, e.g. using images instead of text.")]
    [SerializeField]
    private UpdateBindingUIEvent m_UpdateBindingUIEvent;

    [Tooltip("Event that is triggered when an interactive rebind is being initiated. This can be used, for example, "
        + "to implement custom UI behavior while a rebind is in progress. It can also be used to further "
        + "customize the rebind.")]
    [SerializeField]
    private InteractiveRebindEvent m_RebindStartEvent;

    [Tooltip("Event that is triggered when an interactive rebind is complete or has been aborted.")]
    [SerializeField]
    private InteractiveRebindEvent m_RebindStopEvent;

    private InputActionRebindingExtensions.RebindingOperation m_RebindOperation;

    private static List<RebindActionUI> s_RebindActionUIs;

    // We want the label for the action name to update in edit mode, too, so
    // we kick that off from here.
#if UNITY_EDITOR
    protected void OnValidate()
    {
        UpdateActionLabel();
        UpdateBindingDisplay();
    }

#endif

    private void UpdateActionLabel()
    {
        if (m_ActionLabel != null)
        {
            var action = m_Action?.action;

            //This is where we do the OVERRIDE to make the text our custom text or the default inputaction name --------------------------------------------------------------------New 
            if (m_OverrideActionLable)
            {
                m_ActionLabel.text = m_ActionsLabelOverideString;
            }
            else
            {//--------------------------------------------------------------------New 
                m_ActionLabel.text = action != null ? action.name : string.Empty;
                m_ActionsLabelOverideString = string.Empty;// Delete the string if disabled //--------------------------------------------------------------------New 
            }//end else if OVERRIDE 
        }
    }

    [Serializable]
    public class UpdateBindingUIEvent : UnityEvent<RebindActionUI, string, string, string>
    {
    }

    [Serializable]
    public class InteractiveRebindEvent : UnityEvent<RebindActionUI, InputActionRebindingExtensions.RebindingOperation>
    {
    }
}
