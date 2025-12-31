using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace LlamaBrain.Runtime.Demo.UI
{
    /// <summary>
    /// Utility class for loading scenes and managing application lifecycle in demo scenes.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        /// <summary>
        /// Loads a scene by its build index.
        /// </summary>
        /// <param name="sceneIndex">The build index of the scene to load</param>
        public void LoadScene(int sceneIndex)
        {
            SceneManager.LoadScene(sceneIndex, LoadSceneMode.Single);
        }

        /// <summary>
        /// Loads a scene by its name.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load</param>
        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        /// <summary>
        /// Quits the application. In the editor, stops play mode; in builds, exits the application.
        /// </summary>
        public void Quit()
        {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Opens the LlamaBrain Discord server in the default web browser.
        /// </summary>
        public void OpenDiscord()
        {
            Application.OpenURL("https://discord.gg/9ruBad4nrN");
        }
    }
}
