using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemSequencePresenter : MonoBehaviour
{
    [Header("Item Source")]
    [SerializeField] private List<ItemData> items = new();
    [SerializeField] private bool shuffleOnStart = true;

    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button startButton;

    [Header("Piles")]
    [SerializeField] private int pileCount = 6;

    [Header("Timing")]
    [SerializeField] private float nextItemDelay = 1.5f;

    public int CurrentIndex { get; private set; } = -1;
    public ItemData CurrentItem { get; private set; }

    /// <summary>
    /// Key = pile number (1..pileCount), Value = items sorted into that pile.
    /// </summary>
    public Dictionary<int, List<ItemData>> Piles { get; private set; } = new();

    public event Action<ItemData> OnItemPresented;
    public event Action<ItemData, int> OnItemSorted;
    public event Action<Dictionary<int, List<ItemData>>> OnSequenceFinished;

    private System.Random rng = new System.Random();
    private bool sequenceFinished = false;
    private bool hasStarted = false;
    private bool isWaitingForNext = false;

    private void Start()
    {
        if (items == null || items.Count == 0)
        {
            Debug.LogError("No items assigned to ItemSequencePresenter.");
            return;
        }

        if (pileCount < 1) pileCount = 1;

        // Initialize piles
        Piles.Clear();
        for (int i = 1; i <= pileCount; i++)
            Piles[i] = new List<ItemData>();

        if (shuffleOnStart) Shuffle(items);

        // Start with a completely blank UI
        ClearUI();

        if (startButton)
        {
            startButton.onClick.AddListener(StartGame);
            startButton.gameObject.SetActive(true);
        }
    }

    // Space works only after Start
    private void Update()
    {
        if (!hasStarted || sequenceFinished || isWaitingForNext) return;

        if (Input.GetKeyDown(KeyCode.Space))
            PresentNext();
    }

    // ================= START FLOW =================

    public void StartGame()
    {
        if (hasStarted) return;

        hasStarted = true;

        if (startButton)
            startButton.gameObject.SetActive(false);

        StartCoroutine(StartAfterDelay());
    }

    private System.Collections.IEnumerator StartAfterDelay()
    {
        isWaitingForNext = true;
        yield return new WaitForSeconds(nextItemDelay);
        isWaitingForNext = false;
        PresentNext();
    }

    // ================= GAME FLOW =================

    public void PresentNext()
    {
        if (sequenceFinished || isWaitingForNext) return;

        CurrentIndex++;

        if (CurrentIndex >= items.Count)
        {
            ClearUI();
            CurrentItem = null;
            sequenceFinished = true;

            Debug.Log("Sequence finished.");
            PrintPiles();
            OnSequenceFinished?.Invoke(Piles);
            return;
        }

        CurrentItem = items[CurrentIndex];
        UpdateUI(CurrentItem);
        OnItemPresented?.Invoke(CurrentItem);
    }

    // Called by SortInput script
    public void SortCurrentIntoPile(int pileNumber)
    {
        if (!hasStarted || sequenceFinished || isWaitingForNext) return;
        if (CurrentItem == null) return;

        if (!Piles.ContainsKey(pileNumber))
        {
            Debug.LogWarning($"Invalid pile number: {pileNumber}. Valid range: 1..{pileCount}");
            return;
        }

        ItemData itemToSort = CurrentItem;
        CurrentItem = null;
        isWaitingForNext = true;

        Piles[pileNumber].Add(itemToSort);

        OnItemSorted?.Invoke(itemToSort, pileNumber);
        Debug.Log($"Sorted '{itemToSort.displayName}' into pile {pileNumber}.");

        StartCoroutine(DelayedNextItem());
    }

    private System.Collections.IEnumerator DelayedNextItem()
    {
        yield return new WaitForSeconds(nextItemDelay);
        isWaitingForNext = false;
        PresentNext();
    }

    // ================= UI =================

    private void UpdateUI(ItemData item)
    {
        if (nameText) nameText.text = item.displayName;
        if (descriptionText) descriptionText.text = item.description ?? "";

        if (iconImage)
        {
            iconImage.enabled = item.icon != null;
            iconImage.sprite = item.icon;
        }
    }

    private void ClearUI()
    {
        if (nameText) nameText.text = "";
        if (descriptionText) descriptionText.text = "";
        if (iconImage) iconImage.enabled = false;
    }

    private void Shuffle(List<ItemData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ================= PRINTING =================

    private void PrintPiles()
    {
        for (int i = 1; i <= pileCount; i++)
        {
            Debug.Log(BuildPileString($"Pile {i}", Piles[i]));
        }
    }

    private string BuildPileString(string title, List<ItemData> pile)
    {
        if (pile == null || pile.Count == 0) return $"{title}: (empty)";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"{title} ({pile.Count} items):");
        foreach (var item in pile)
            sb.AppendLine($"- {item.displayName}");

        return sb.ToString();
    }
}
