using TMPro;
using UnityEngine;

public class WinnerUI : MonoBehaviour
{
    [SerializeField] private GradeClient gradeClient;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private TMP_Text reasonText;
    [SerializeField] private TMP_Text scoreAText;
    [SerializeField] private TMP_Text scoreBText;


    private void OnEnable()
    {
        if (!gradeClient)
            gradeClient = FindFirstObjectByType<GradeClient>();

        if (gradeClient)
            gradeClient.OnResultReceived += Show;
    }

    private void OnDisable()
    {
        if (gradeClient)
            gradeClient.OnResultReceived -= Show;
    }

    private void Show(WinnerResult r)
    {
        gameObject.SetActive(true);

        if (winnerText)
        {
            winnerText.text = r.winner == "Tie"
                ? "TIE GAME"
                : $"PLAYER {r.winner} WINS";
        }

        if (scoreAText)
            scoreAText.text = $"Player A: {r.scoreA:0}";

        if (scoreBText)
            scoreBText.text = $"Player B: {r.scoreB:0}";

        if (reasonText)
            reasonText.text = r.reason ?? "";
    }

}
