using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// This namespace contains the UI components for the UnityBrainDemo project.
/// </summary>
namespace UnityBrainDemo.Runtime.UI
{
    /// <summary>
    /// A component that displays a message in a dialogue.
    /// </summary>
    public class DialogueMessage : MonoBehaviour
    {
        /// <summary>
        /// The text component that displays the message.
        /// </summary>
        [SerializeField] private TextMeshProUGUI messageText;

        /// <summary>
        /// Sets the message to display.
        /// </summary>
        public void SetMessage(string message)
        {
            messageText.text = message;
        }

        /// <summary>
        /// Clears the message.
        /// </summary>
        public void Clear()
        {
            messageText.text = string.Empty;
        }
    }
}