using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using LlamaBrain.Runtime.Demo;
using LlamaBrain.Runtime.Core.Voice;
using UnityEngine.SceneManagement;

namespace LlamaBrain.Runtime.Demo.UI
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

        [Header("Voice Settings")]
        [SerializeField]
        [Tooltip("Optional voice controller for voice-enabled conversations.")]
        private NpcVoiceController voiceController;

        [SerializeField]
        [Tooltip("Seconds of silence before auto-closing dialogue.")]
        private float silenceTimeout = 6f;

        /// <summary>
        /// The event that is triggered when the player's message is submitted.
        /// </summary>
        [Header("Events")]
        [SerializeField] private UnityEvent<string> onPlayerMessageSubmitted;

        [SerializeField]
        [Tooltip("Fired when dialogue is closed due to silence timeout.")]
        private UnityEvent onDialogueClosed;

        private bool _isVoiceMode;
        private float _lastActivityTime;

        /// <summary>
        /// Called when the dialogue panel is created.
        /// </summary>
        private void Awake()
        {
            submitButton.onClick.AddListener(OnButtonSubmit);
            inputField.onSubmit.AddListener(OnInputSubmit);

            // Wire up voice controller events if present
            if (voiceController != null)
            {
                voiceController.OnPlayerSpeechRecognized.AddListener(OnPlayerSpeechRecognized);
                voiceController.OnNpcResponseGenerated.AddListener(OnNpcResponseGenerated);
                voiceController.OnSpeakingFinished.AddListener(OnVoiceActivityChanged);
                voiceController.OnListeningStarted.AddListener(OnVoiceActivityChanged);
                voiceController.OnSilenceTimeout.AddListener(OnSilenceTimeoutReached);
            }
        }

        /// <summary>
        /// Called when the dialogue panel is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            submitButton.onClick.RemoveListener(OnButtonSubmit);
            inputField.onSubmit.RemoveListener(OnInputSubmit);

            // Unsubscribe from voice controller events
            if (voiceController != null)
            {
                voiceController.OnPlayerSpeechRecognized.RemoveListener(OnPlayerSpeechRecognized);
                voiceController.OnNpcResponseGenerated.RemoveListener(OnNpcResponseGenerated);
                voiceController.OnSpeakingFinished.RemoveListener(OnVoiceActivityChanged);
                voiceController.OnListeningStarted.RemoveListener(OnVoiceActivityChanged);
                voiceController.OnSilenceTimeout.RemoveListener(OnSilenceTimeoutReached);
            }
        }

        private void Update()
        {
            // Check silence timeout in voice mode
            if (_isVoiceMode && voiceController != null)
            {
                if (!voiceController.IsSpeaking && !voiceController.IsListening && !voiceController.IsProcessing)
                {
                    if (Time.time - _lastActivityTime > silenceTimeout)
                    {
                        CloseDialogue();
                    }
                }
            }
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
        /// <summary>
        /// Returns to the main menu by loading scene 0.
        /// </summary>
        public void BackToMainMenu()
        {
            SceneManager.LoadScene(0, LoadSceneMode.Single);
        }

        /// <summary>
        /// Opens the dialogue panel with optional voice mode.
        /// </summary>
        /// <param name="useVoice">Whether to enable voice mode.</param>
        public void OpenDialogue(bool useVoice = false)
        {
            gameObject.SetActive(true);
            _isVoiceMode = useVoice;
            ResetSilenceTimer();

            if (useVoice && voiceController != null)
            {
                voiceController.StartConversation();
            }
        }

        /// <summary>
        /// Closes the dialogue panel.
        /// </summary>
        public void CloseDialogue()
        {
            if (_isVoiceMode && voiceController != null)
            {
                voiceController.EndConversation();
            }

            _isVoiceMode = false;
            onDialogueClosed?.Invoke();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets the voice controller for voice-enabled conversations.
        /// </summary>
        public void SetVoiceController(NpcVoiceController controller)
        {
            // Unsubscribe from old controller
            if (voiceController != null)
            {
                voiceController.OnPlayerSpeechRecognized.RemoveListener(OnPlayerSpeechRecognized);
                voiceController.OnNpcResponseGenerated.RemoveListener(OnNpcResponseGenerated);
                voiceController.OnSpeakingFinished.RemoveListener(OnVoiceActivityChanged);
                voiceController.OnListeningStarted.RemoveListener(OnVoiceActivityChanged);
                voiceController.OnSilenceTimeout.RemoveListener(OnSilenceTimeoutReached);
            }

            voiceController = controller;

            // Subscribe to new controller
            if (voiceController != null)
            {
                voiceController.OnPlayerSpeechRecognized.AddListener(OnPlayerSpeechRecognized);
                voiceController.OnNpcResponseGenerated.AddListener(OnNpcResponseGenerated);
                voiceController.OnSpeakingFinished.AddListener(OnVoiceActivityChanged);
                voiceController.OnListeningStarted.AddListener(OnVoiceActivityChanged);
                voiceController.OnSilenceTimeout.AddListener(OnSilenceTimeoutReached);
            }
        }

        private void OnPlayerSpeechRecognized(string text)
        {
            AddPlayerMessage(text);
            ResetSilenceTimer();
        }

        private void OnNpcResponseGenerated(string text)
        {
            AddNpcMessage(text);
            ResetSilenceTimer();
        }

        private void OnVoiceActivityChanged()
        {
            ResetSilenceTimer();
        }

        private void OnSilenceTimeoutReached()
        {
            CloseDialogue();
        }

        private void ResetSilenceTimer()
        {
            _lastActivityTime = Time.time;
        }

        /// <summary>
        /// Whether the dialogue is in voice mode.
        /// </summary>
        public bool IsVoiceMode => _isVoiceMode;

        /// <summary>
        /// The current voice controller.
        /// </summary>
        public NpcVoiceController VoiceController => voiceController;
    }
}