using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NetHudUI : MonoBehaviour
{
    public TMP_Text timerText;
    public TMP_Text hpText;

    private NetGameManager gm;
    private NetHealth myHp;

    private void Start()
    {
        gm = FindFirstObjectByType<NetGameManager>();
    }

    private void Update()
    {
        if (gm != null && timerText != null)
        {
            float t = gm.TimeLeft.Value;
            int sec = Mathf.CeilToInt(t);
            timerText.text = $"Time: {sec}s";
        }

        if (myHp == null)
            myHp = FindMyHealth();

        if (myHp != null && hpText != null)
            hpText.text = $"HP: {myHp.Hp.Value}";
    }

    private NetHealth FindMyHealth()
    {
        foreach (var h in FindObjectsByType<NetHealth>(FindObjectsSortMode.None))
        {
            var no = h.GetComponent<NetworkObject>();
            if (no != null && no.IsOwner)
                return h;
        }
        return null;
    }
}
