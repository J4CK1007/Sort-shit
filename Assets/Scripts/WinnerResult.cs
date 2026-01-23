using System;

[Serializable]
public class WinnerResult
{
    public string winner;   // "A", "B", or "Tie"
    public float scoreA;
    public float scoreB;
    public string reason;
}
