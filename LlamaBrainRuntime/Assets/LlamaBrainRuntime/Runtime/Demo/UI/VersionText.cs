using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LlamaBrain.Runtime.Demo.UI
{
    /// <summary>
    /// Displays the application version number in a TextMeshPro text component.
    /// </summary>
    public class VersionText : MonoBehaviour
    {
        [SerializeField] private TMP_Text versionText;
        // Start is called before the first frame update
        void Start()
        {
            versionText.text = $"v{Application.version}";
        }
    }
}
