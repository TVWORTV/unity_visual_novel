using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour, IPointerClickHandler
{
    public Action onDialogueStart, onDialogueEnd;
    public Action<TextPart> onPartEnter, onPartExit;
    public Action<string> onLineFinished;
    public Action<IReadOnlyList<ConditionEntry>> onOptionsShown;
    public Action<ConditionEntry> onOptionChosen;

    public TextManager textManager;
    public WritingStyle writingStyle;
    public TMP_Text dialogueText;
    public TMP_Text nameText;
    public string defaultSpeaker = "";

    bool ignoreWritingEnd;
    bool warnedInstant;
    string currentLine = string.Empty;

    void Awake()
    {
        if (textManager == null) textManager = FindFirstObjectByType<TextManager>();
        if (dialogueText == null && textManager != null) dialogueText = textManager.BoundText;
        HideButtons();
    }

    void OnEnable()
    {
        if (textManager != null) textManager.onWritingEnd += OnWritingEnded;
    }

    void OnDisable()
    {
        if (textManager != null) textManager.onWritingEnd -= OnWritingEnded;
    }

    public void StartDialogue(FullTextAsset textAsset)
    {
        if (textAsset == null)
        {
            Debug.LogError("StartDialogue needs a dialogue asset");
            return;
        }

        if (isRunning) StopDialogue();

        currentAsset = textAsset;
        onDialogueStart?.Invoke();
        EnterPart(textAsset.entry);
    }

    public void StopDialogue()
    {
        if (!isRunning) return;
        EnterPart(null);
    }

#region current 

    public FullTextAsset currentAsset;
    public TextPart currentPart = null;
    public bool options => currentPart is ConditionTable;
    public bool isRunning => currentAsset != null;

#endregion

#region Flow

    void EnterPart(TextPart part)
    {
        if (currentPart != null)
        {
            TextPart leaving = currentPart;
            leaving.OnExit();
            onPartExit?.Invoke(leaving);
        }

        currentPart = part;
        HideButtons();

        if (part == null)
        {
            FinishDialogue();
            return;
        }

        part.OnEnter();
        if (currentPart != part) return;

        onPartEnter?.Invoke(part);
        if (currentPart != part) return;

        switch (part)
        {
            case SimpleText simple:
                ShowText(simple);
                break;

            case ConditionTable table:
                OnConditionTableEnter(table);
                break;

            case EntryPart entry:
                EnterPart(entry.GetNext());
                break;

            default:
                Debug.LogWarning("unsupported part type " + part.GetType().Name);
                EnterPart(null);
                break;
        }
    }

    void Advance()
    {
        if (currentPart is SimpleText simple) EnterPart(simple.GetNext());
    }

    void FinishDialogue()
    {
        StopWriting();
        currentAsset = null;
        finishedWriting = false;
        currentLine = string.Empty;
        if (dialogueText != null) dialogueText.text = string.Empty;
        SetName(string.Empty);
        onDialogueEnd?.Invoke();
    }

#endregion

#region Text State 

    public bool finishedWriting;

    void ShowText(SimpleText part)
    {
        SetName(GetSpeaker(part));

        currentLine = part.line ?? string.Empty;
        finishedWriting = false;

        if (string.IsNullOrWhiteSpace(currentLine))
        {
            StopWriting();
            if (dialogueText != null) dialogueText.text = string.Empty;
            CompleteLine();
            return;
        }

        if (textManager == null || writingStyle == null || dialogueText == null)
        {
            if (!warnedInstant)
            {
                warnedInstant = true;
                Debug.LogWarning("DialogueManager is missing a TextManager, WritingStyle or dialogue text, lines will appear instantly");
            }

            StopWriting();
            if (dialogueText != null) dialogueText.text = currentLine;
            CompleteLine();
            return;
        }

        ignoreWritingEnd = true;
        try
        {
            textManager.Write(currentLine, dialogueText, writingStyle);
        }
        finally
        {
            ignoreWritingEnd = false;
        }
    }

    void CompleteLine()
    {
        finishedWriting = true;
        onLineFinished?.Invoke(currentLine);
    }

    void StopWriting()
    {
        if (textManager == null || !textManager.IsBusy) return;
        if (textManager.BoundText != dialogueText) return;
        textManager.Stop();
    }

    void OnWritingEnded()
    {
        if (ignoreWritingEnd || !isRunning || finishedWriting) return;
        if (textManager.BoundText != dialogueText) return;
        CompleteLine();
    }

    string GetSpeaker(SimpleText part)
    {
        return defaultSpeaker;
    }

    void SetName(string value)
    {
        if (nameText != null) nameText.text = value;
    }

#endregion

#region Interaction

    public List<Button> buttons = new(); 

    readonly List<ConditionEntry> visibleEntries = new();

    public IReadOnlyList<ConditionEntry> VisibleOptions => visibleEntries;

    public void OnConditionTableEnter(ConditionTable part)
    {
        HideButtons();

        foreach (ConditionEntry entry in part.entries)
        {
            if (!entry.True()) continue;

            int index = visibleEntries.Count;
            if (index >= buttons.Count || buttons[index] == null)
            {
                Debug.LogWarning("not enough option buttons for this condition table");
                break;
            }

            Button button = buttons[index];
            ConditionEntry chosen = entry;

            button.gameObject.SetActive(true);
            button.onClick.AddListener(() => OnOptionChosen(chosen));
            SetButtonLabel(button, entry, index);

            visibleEntries.Add(entry);
        }

        if (visibleEntries.Count == 0)
        {
            Debug.LogWarning("condition table has no available options, ending dialogue");
            EnterPart(null);
            return;
        }

        onOptionsShown?.Invoke(visibleEntries);
    }

    public bool ChooseOption(int index)
    {
        if (!options || index < 0 || index >= visibleEntries.Count) return false;

        OnOptionChosen(visibleEntries[index]);
        return true;
    }

    void OnOptionChosen(ConditionEntry entry)
    {
        if (!options) return;

        onOptionChosen?.Invoke(entry);
        EnterPart(entry.leadsTo);
    }

    void SetButtonLabel(Button button, ConditionEntry entry, int index)
    {
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null) return;

        bool hasLine = entry.leadsTo is SimpleText simple && !string.IsNullOrWhiteSpace(simple.line);
        label.text = hasLine ? ((SimpleText)entry.leadsTo).line : "Option " + (index + 1);
    }

    void HideButtons()
    {
        visibleEntries.Clear();

        foreach (Button button in buttons)
        {
            if (button == null) continue;
            button.onClick.RemoveAllListeners();
            button.gameObject.SetActive(false);
        }
    }

    public void OnTextClick()
    {
        if (!isRunning || options) return;

        if (!finishedWriting)
        {
            if (textManager != null && textManager.IsBusy) textManager.Skip();
            else CompleteLine();
            return;
        }

        Advance();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (textManager != null && textManager.IsOverLink(eventData)) return;

        OnTextClick();
    }

#endregion

}
