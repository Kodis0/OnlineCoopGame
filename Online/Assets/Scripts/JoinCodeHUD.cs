using UnityEngine;
using UnityEngine.UI;

public class JoinCodeHUD : MonoBehaviour
{
    public Text text;

    private void Update()
    {
        if (JoinCodeStore.Instance == null) { text.text = ""; return; }

        var code = JoinCodeStore.Instance.JoinCode;
        text.text = string.IsNullOrEmpty(code) ? "" : ("CODE: " + code);
    }
}
