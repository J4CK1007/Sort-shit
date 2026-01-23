require("dotenv").config();

const express = require("express");
const cors = require("cors");

const app = express();
app.use(cors());
app.use(express.json({ limit: "1mb" }));

console.log("SERVER VERSION: v4-head-to-head-A123-vs-B456");
console.log("API KEY LOADED:", process.env.OPENAI_API_KEY ? "YES" : "NO");

app.get("/health", (req, res) => {
  res.json({ ok: true, version: "v4-head-to-head-A123-vs-B456" });
});

// Helper: normalize request piles into a predictable shape:
// Accepts either:
// piles: [ { pileId: "1", items: ["a","b"] }, ... ]
// or piles: { "1": ["a","b"], "2": ["c"] ... }
function normalizePiles(piles) {
  if (Array.isArray(piles)) {
    return piles.map(p => ({
      pileId: String(p.pileId ?? ""),
      items: Array.isArray(p.items) ? p.items.map(String) : []
    }));
  }

  if (piles && typeof piles === "object") {
    return Object.keys(piles).map(k => ({
      pileId: String(k),
      items: Array.isArray(piles[k]) ? piles[k].map(String) : []
    }));
  }

  return null;
}

app.post("/grade", async (req, res) => {
  try {
    // Optional: see what Unity sent
    console.log("=== /grade received body ===");
    console.log(JSON.stringify(req.body, null, 2));

    const pilesRaw = req.body?.piles;
    if (!pilesRaw) {
      return res.status(400).json({ error: "Missing 'piles' in request body" });
    }

    const piles = normalizePiles(pilesRaw);
    if (!piles) {
      return res.status(400).json({ error: "Invalid 'piles' format" });
    }

    // Ensure we always judge exactly 6 piles (1..6). Fill missing with empty.
    const byId = new Map(piles.map(p => [String(p.pileId), p.items]));
    const sixPiles = ["1","2","3","4","5","6"].map(id => ({
      pileId: id,
      items: byId.get(id) ?? []
    }));

    // Head-to-head prompt (Player A = 1-3, Player B = 4-6)
    const prompt = `
You are judging a sorting game match between two players.

Player A controls piles 1, 2, 3.
Player B controls piles 4, 5, 6.

Each pile contains item names only (strings).
Judge which player sorted better.

Criteria:
- Items within each pile should belong together by a sensible inferred category.
- A player's three piles should be meaningfully different from each other.
- Empty piles are bad.
- Mixing unrelated items is bad.
- Find every possible connection that items might have in a reasonable way
- The more niche the category or connection is, the higher you score

Return ONLY valid JSON.
Do NOT use markdown.
Do NOT use backticks.
Do NOT add explanations outside JSON.

Return JSON in EXACTLY this schema:
{
  "winner": "A" | "B" | "Tie",
  "scoreA": number,
  "scoreB": number,
  "reason": string
}

Rules:
- Scores must be 0–100.
- Higher score must win.
- Use "Tie" if scores are the same.
- Reason must be short (1–3 sentences).
- Do NOT invent items.

Piles:
${JSON.stringify(sixPiles, null, 2)}
`;

    const response = await fetch("https://api.openai.com/v1/responses", {
      method: "POST",
      headers: {
        "Authorization": `Bearer ${process.env.OPENAI_API_KEY}`,
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        model: "gpt-4.1-mini",
        input: prompt,
        temperature: 0
      })
    });

    if (!response.ok) {
      const errText = await response.text();
      console.log("OpenAI ERROR:", errText);
      return res.status(response.status).send(errText);
    }

    const data = await response.json();

    // Extract assistant text from Responses API
    const msg = data.output?.find(o => o.type === "message");
    const outputText = msg?.content?.find(c => c.type === "output_text")?.text || "";

    // Clean accidental fences
    const cleanedText = outputText
      .replace(/```json\s*/gi, "")
      .replace(/```/g, "")
      .trim();

    let result;
    try {
      result = JSON.parse(cleanedText);
    } catch (e) {
      console.log("JSON PARSE FAILED. RAW OUTPUT:\n", outputText);
      return res.status(500).json({ error: "Model did not return valid JSON", raw: outputText });
    }

    // Safety normalization for winner schema
    const w = String(result.winner ?? "").toUpperCase();
    result.winner = (w === "A" || w === "B") ? w : "Tie";
    result.scoreA = Number(result.scoreA ?? 0);
    result.scoreB = Number(result.scoreB ?? 0);
    result.reason = String(result.reason ?? "");

    return res.json(result);

  } catch (err) {
    console.log("SERVER ERROR:", err);
    res.status(500).json({ error: err.toString() });
  }
});

const port = process.env.PORT || 3000;
app.listen(port, () => {
  console.log("Server running on http://localhost:" + port);
});
