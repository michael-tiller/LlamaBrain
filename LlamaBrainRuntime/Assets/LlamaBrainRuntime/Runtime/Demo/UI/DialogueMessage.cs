using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// This namespace contains the UI components for the LlamaBrain project.
/// </summary>
namespace LlamaBrain.Runtime.Demo.UI
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
        /// <param name="message">The message text to display</param>
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