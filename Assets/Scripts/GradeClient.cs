using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GradeClient : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemSequencePresenter presenter;

    [Header("Server")]
    [SerializeField] private string gradeUrl = "http://localhost:3000/grade";
    [SerializeField] private int pileCount = 6;
    [SerializeField] private int timeoutSeconds = 15;

    // Head-to-head result (A = piles 1-3, B = piles 4-6)
    public event Action<WinnerResult> OnResultReceived;

    private void Reset()
    {
        presenter = FindFirstObjectByType<ItemSequencePresenter>();
    }

    private void OnEnable()
    {
        if (!presenter) presenter = FindFirstObjectByType<ItemSequencePresenter>();
        if (presenter) presenter.OnSequenceFinished += HandleFinished;
    }

    private void OnDisable()
    {
        if (presenter) presenter.OnSequenceFinished -= HandleFinished;
    }

    private void HandleFinished(Dictionary<int, List<ItemData>> piles)
    {
        string json = BuildRequestJson(piles);
        Debug.Log("[GradeClient] Sending:\n" + json);
        StartCoroutine(PostJson(gradeUrl, json));
    }

    // Builds:
    // { "piles": { "1": ["Watch"], "2": [], ... } }
    private string BuildRequestJson(Dictionary<int, List<ItemData>> piles)
    {
        var sb = new StringBuilder();
        sb.Append("{\"piles\":{");

        for (int i = 1; i <= pileCount; i++)
        {
            if (i > 1) sb.Append(",");

            sb.Append("\"").Append(i).Append("\":[");

            if (piles != null && piles.TryGetValue(i, out var list) && list != null && list.Count > 0)
            {
                for (int j = 0; j < list.Count; j++)
                {
                    if (j > 0) sb.Append(",");

                    string name = list[j] != null ? (list[j].displayName ?? "") : "";
                    sb.Append("\"").Append(EscapeJson(name)).Append("\"");
                }
            }

            sb.Append("]");
        }

        sb.Append("}}");
        return sb.ToString();
    }

    private IEnumerator PostJson(string url, string json)
    {
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = timeoutSeconds;

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[GradeClient] Failed: HTTP {req.responseCode}\n{req.error}\n{req.downloadHandler.text}");
            yield break;
        }

        string responseText = req.downloadHandler.text;
        Debug.Log("[GradeClient] Response:\n" + responseText);

        WinnerResult parsed = null;
        try
        {
            parsed = JsonUtility.FromJson<WinnerResult>(responseText);
        }
        catch (Exception e)
        {
            Debug.LogError("[GradeClient] JSON parse failed: " + e.Message);
        }

        if (parsed != null)
        {
            OnResultReceived?.Invoke(parsed);
        }
    }

    private static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
    }
}

