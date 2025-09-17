using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LlamaBrain.Runtime.Demo.UI
{
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
