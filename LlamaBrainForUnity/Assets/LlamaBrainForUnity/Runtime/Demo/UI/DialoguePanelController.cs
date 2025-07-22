using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using LlamaBrain.Unity.Runtime.Demo;

namespace LlamaBrain.Unity.Runtime.Demo.UI
{
    /// <summary>
    /// A controller for the dialogue panel.
    /// </summary>
    public class DialoguePanelController : MonoBehaviour
    {
        /// <summary>
        /// The button to submit the player's message.
        /// </summary>
        [SerializeField] private Button submitButton;

        /// <summary>
        /// The input field for the player's message.
        /// </summary>
        [SerializeField] private TMP_InputField inputField;

        /// <summary>
        /// The prefab for the NPC's message.
        /// </summary>
        [SerializeField] private DialogueMessage npcMessageTextPrefab;

        /// <summary>
        /// The prefab for the player's message.
        /// </summary>
        [SerializeField] private DialogueMessage playerMessageTextPrefab;

        /// <summary>
        /// The container for the dialogue messages.
        /// </summary>
        [SerializeField] private Transform dialogueMessageContainer;


        /// <summary>
        /// The event that is triggered when the player's message is submitted.
        /// </summary>
        [Header("Events")]
        [SerializeField] private UnityEvent<string> onPlayerMessageSubmitted;

        /// <summary>
        /// Called when the dialogue panel is created.
        /// </summary>
        private void Awake()
        {
            submitButton.onClick.AddListener(OnButtonSubmit);
            inputField.onSubmit.AddListener(OnInputSubmit);
        }

        /// <summary>
        /// Called when the dialogue panel is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            submitButton.onClick.RemoveListener(OnButtonSubmit);
            inputField.onSubmit.RemoveListener(OnInputSubmit);
        }

        /// <summary>
        /// Called when the submit button is clicked.
        /// </summary>
        private void OnButtonSubmit() => OnSubmit(inputField.text);

        /// <summary>
        /// Called when the input field is submitted.
        /// </summary>
        private void OnInputSubmit(string text) => OnSubmit(text);

        /// <summary>
        /// Called when the input field is submitted.
        /// </summary>
        /// <param name="text">The text to submit.</param>  
        private void OnSubmit(string text = "")
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            AddPlayerMessage(text);
            onPlayerMessageSubmitted?.Invoke(text);
            inputField.text = "";
        }

        /// <summary>
        /// Adds an NPC message to the dialogue.
        /// </summary>
        /// <param name="message">The message to add.</param>
        public void AddNpcMessage(string message)
        {
            Debug.Log($"NPC Message: {message}");

            var npcMessage = Instantiate(npcMessageTextPrefab, dialogueMessageContainer);
            npcMessage.SetMessage(message);
        }

        /// <summary>
        /// Adds a player message to the dialogue.
        /// </summary>
        /// <param name="message">The message to add.</param>
        public void AddPlayerMessage(string message)
        {
            Debug.Log($"Player Message: {message}");

            var playerMessage = Instantiate(playerMessageTextPrefab, dialogueMessageContainer);
            playerMessage.SetMessage(message);
        }
    }
}