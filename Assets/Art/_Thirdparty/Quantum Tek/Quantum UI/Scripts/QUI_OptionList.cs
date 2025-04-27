using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace QuantumTek.QuantumUI
{
    [AddComponentMenu("Quantum Tek/Quantum UI/Option List")]
    [DisallowMultipleComponent]
    public class QUI_OptionList : MonoBehaviour
    {
        [Header("Option List Object References")]
        [Tooltip("The text to show the current option.")]
        public TextMeshProUGUI optionText;

        [Header("Option List Variables")]
        [Tooltip("The list of option choices.")]
        public List<string> options;
        [Tooltip("The event invoked on changing the current option. Now passing the new index.")]
        public UnityEvent<int> onChangeOption;
        [HideInInspector] public int optionIndex;
        [HideInInspector] public string option;

        public void Awake()
        {
            SetOption(optionIndex);
        }

        public void SetOption(int index)
        {
            optionIndex = index;
            option = options[optionIndex];
            optionText.text = option;
        }

        public void ChangeOption(int direction)
        {
            optionIndex += direction;
            if (optionIndex < 0)
                optionIndex = options.Count - 1;
            else if (optionIndex >= options.Count)
                optionIndex = 0;

            SetOption(optionIndex);
            onChangeOption.Invoke(optionIndex);
        }
        
        public string GetCurrentOption()
        {
            return option;
        }
    }
}