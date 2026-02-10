using System.Text;
using TMPro;
using UnityEngine;

public class NetScoreboardUI : MonoBehaviour
{
    public TMP_Text scoreboardText;

    private NetGameManager gm;

    private void Start()
    {
        gm = FindFirstObjectByType<NetGameManager>();
    }

    private void Update()
    {
        if (gm == null || scoreboardText == null) return;

        var sb = new StringBuilder();
        sb.AppendLine("SCORE:");

        for (int i = 0; i < gm.Scoreboard.Count; i++)
        {
            var e = gm.Scoreboard[i];
            sb.AppendLine($"Player {e.ClientId}: {e.Kills}");
        }

        if (!gm.MatchRunning.Value)
            sb.AppendLine("\nMATCH OVER");

        scoreboardText.text = sb.ToString();
    }
}
