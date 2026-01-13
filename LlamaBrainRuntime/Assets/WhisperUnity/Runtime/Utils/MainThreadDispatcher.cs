using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

namespace Whisper.Utils
{
    /// <summary>
    /// Helper class to dispatch events from main thread.
    /// Useful to pass events from other threads to Unity main thread.
    /// This is the original instance-based class for backwards compatibility.
    /// </summary>
    public class MainThreadDispatcher
    {
        private readonly ConcurrentQueue<Task> _actions = new ConcurrentQueue<Task>();

        /// <summary>
        /// Static method to enqueue an action to be executed on main thread.
        /// Auto-initializes the singleton MonoBehaviour if needed.
        /// </summary>
        public static void Enqueue(Action action)
        {
            MainThreadDispatcherSingleton.Enqueue(action);
        }

        /// <summary>
        /// Add action to be executed on main Unity thread.
        /// </summary>
        public void Execute(Action action)
        {
            _actions.Enqueue(new Task(action));
        }

        /// <summary>
        /// Call this in Unity update to flush all pending actions.
        /// </summary>
        public void Update()
        {
            while (_actions.TryDequeue(out var task))
            {
                task.RunSynchronously();
            }
        }
    }

    /// <summary>
    /// MonoBehaviour singleton for static main thread dispatching.
    /// Auto-creates itself on load via RuntimeInitializeOnLoadMethod.
    /// </summary>
    public class MainThreadDispatcherSingleton : MonoBehaviour
    {
        private static MainThreadDispatcherSingleton _instance;
        private static readonly ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();

        /// <summary>
        /// Initialize the singleton on the main thread before any scene loads.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_instance != null) return;

            var go = new GameObject("[MainThreadDispatcher]");
            _instance = go.AddComponent<MainThreadDispatcherSingleton>();
            DontDestroyOnLoad(go);
        }

        /// <summary>
        /// Enqueue an action to be executed on the main thread.
        /// </summary>
        public static void Enqueue(Action action)
        {
            if (action == null) return;
            _actions.Enqueue(action);
            // Don't call EnsureInstance here - it's already initialized via RuntimeInitializeOnLoadMethod
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Update()
        {
            while (_actions.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MainThreadDispatcher] Error executing action: {ex.Message}");
                }
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
