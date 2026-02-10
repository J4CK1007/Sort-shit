using System.Collections;
using TMPro;
using UnityEngine;

public class WinnerUI : MonoBehaviour
{
    [SerializeField] private GradeClient gradeClient;

    [Header("UI")]
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private TMP_Text reasonText;
    [SerializeField] private TMP_Text scoreAText;
    [SerializeField] private TMP_Text scoreBText;

    [Header("Typewriter Timing")]
    [SerializeField] private float charDelay = 0.03f;     // seconds per character
    [SerializeField] private float linePause = 0.35f;     // pause between sections

    private Coroutine revealRoutine;

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

        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }
    }

    private void Show(WinnerResult r)
    {
        gameObject.SetActive(true);

        if (revealRoutine != null)
            StopCoroutine(revealRoutine);

        revealRoutine = StartCoroutine(RevealRoutine(r));
    }

    private IEnumerator RevealRoutine(WinnerResult r)
    {
        // Clear first
        if (winnerText) winnerText.text = "";
        if (scoreAText) scoreAText.text = "";
        if (scoreBText) scoreBText.text = "";
        if (reasonText) reasonText.text = "";

        // 1) Reason first
        string reason = r.reason ?? "";
        if (reasonText)
            yield return TypeLine(reasonText, reason);

        yield return new WaitForSeconds(linePause);

        // 2) Scores + winner at the same time
        string aLine = $"Player A: {r.scoreA:0}";
        string bLine = $"Player B: {r.scoreB:0}";

        string finalWinner = (r.winner == "Tie")
            ? "TIE GAME"
            : $"PLAYER {r.winner} WINS";

        Coroutine aCo = null;
        Coroutine bCo = null;
        Coroutine wCo = null;

        if (scoreAText) aCo = StartCoroutine(TypeLine(scoreAText, aLine));
        if (scoreBText) bCo = StartCoroutine(TypeLine(scoreBText, bLine));
        if (winnerText) wCo = StartCoroutine(TypeLine(winnerText, finalWinner));

        // Wait for all running typewriters to finish
        if (aCo != null) yield return aCo;
        if (bCo != null) yield return bCo;
        if (wCo != null) yield return wCo;
    }

    private IEnumerator TypeLine(TMP_Text t, string full)
    {
        t.text = "";
        for (int i = 0; i < full.Length; i++)
        {
            t.text += full[i];
            yield return new WaitForSeconds(charDelay);
        }
    }
}
