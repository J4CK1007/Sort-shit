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
    [Tooltip("Full /grade endpoint URL")]
    [SerializeField] private string gradeUrl = "https://graderbackend-c8wc.onrender.com/grade";

    [Tooltip("Optional /health endpoint URL (recommended)")]
    [SerializeField] private string healthUrl = "https://graderbackend-c8wc.onrender.com/health";

    [SerializeField] private int pileCount = 6;
    [SerializeField] private int timeoutSeconds = 20;

    [Header("Debug")]
    [SerializeField] private bool pingHealthOnStart = true;
    [SerializeField] private bool logRequestBody = true;

    // Head-to-head result (A = piles 1-3, B = piles 4-6)
    public event Action<WinnerResult> OnResultReceived;

    private void Reset()
    {
        presenter = FindFirstObjectByType<ItemSequencePresenter>();
    }

    private void Start()
    {
        gradeUrl = NormalizeUrl(gradeUrl);
        healthUrl = NormalizeUrl(healthUrl);

        if (pingHealthOnStart && !string.IsNullOrWhiteSpace(healthUrl))
            StartCoroutine(GetHealth(healthUrl));
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
        if (string.IsNullOrWhiteSpace(gradeUrl))
        {
            Debug.LogError("[GradeClient] gradeUrl is empty. Set it in the Inspector.");
            return;
        }

        string json = BuildRequestJson(piles);

        if (logRequestBody)
            Debug.Log("[GradeClient] POST " + gradeUrl + "\n[GradeClient] Body:\n" + json);

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

            bool first = true;

            if (piles != null && piles.TryGetValue(i, out var list) && list != null && list.Count > 0)
            {
                for (int j = 0; j < list.Count; j++)
                {
                    string name = ExtractItemName(list[j]);
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    if (!first) sb.Append(",");
                    first = false;

                    sb.Append("\"").Append(EscapeJson(name)).Append("\"");
                }
            }

            sb.Append("]");
        }

        sb.Append("}}");
        return sb.ToString();
    }

    private static string ExtractItemName(ItemData item)
    {
        if (item == null) return "";
        // Your ItemSequencePresenter uses DisplayName, so we do too.
        return (item.DisplayName ?? "").Trim();
    }

    private IEnumerator PostJson(string url, string json)
    {
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = timeoutSeconds;

        yield return req.SendWebRequest();

        string body = req.downloadHandler != null ? req.downloadHandler.text : "";

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[GradeClient] Failed: HTTP {req.responseCode}\n{req.error}\n{body}");
            yield break;
        }

        Debug.Log("[GradeClient] Response:\n" + body);

        WinnerResult parsed;
        try
        {
            parsed = JsonUtility.FromJson<WinnerResult>(body);
        }
        catch (Exception e)
        {
            Debug.LogError("[GradeClient] JSON parse failed: " + e.Message + "\nRaw:\n" + body);
            yield break;
        }

        if (parsed == null)
        {
            Debug.LogError("[GradeClient] Parsed result is null.\nRaw:\n" + body);
            yield break;
        }

        // Defensive normalization so WinnerUI never breaks
        parsed.winner = NormalizeWinner(parsed.winner);
        parsed.scoreA = Clamp01_100(parsed.scoreA);
        parsed.scoreB = Clamp01_100(parsed.scoreB);
        parsed.reason = parsed.reason ?? "";

        OnResultReceived?.Invoke(parsed);
    }

    [ContextMenu("Ping /health")]
    public void PingHealth()
    {
        if (string.IsNullOrWhiteSpace(healthUrl))
        {
            Debug.LogWarning("[GradeClient] healthUrl is empty.");
            return;
        }

        StartCoroutine(GetHealth(NormalizeUrl(healthUrl)));
    }

    private IEnumerator GetHealth(string url)
    {
        using var req = UnityWebRequest.Get(url);
        req.timeout = Mathf.Max(5, timeoutSeconds);

        Debug.Log("[GradeClient] GET " + url);

        yield return req.SendWebRequest();

        string body = req.downloadHandler != null ? req.downloadHandler.text : "";

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[GradeClient] /health failed: HTTP {req.responseCode}\n{req.error}\n{body}");
            yield break;
        }

        Debug.Log("[GradeClient] /health OK:\n" + body);
    }

    private static float Clamp01_100(float v)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) return 0f;
        return Mathf.Clamp(v, 0f, 100f);
    }

    private static string NormalizeWinner(string w)
    {
        if (string.IsNullOrWhiteSpace(w)) return "Tie";
        w = w.Trim().ToUpperInvariant();

        if (w == "A") return "A";
        if (w == "B") return "B";
        return "Tie";
    }

    private static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        return url.Trim();
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
