using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour, IPointerClickHandler
{
    public Action onDialogueStart, onDialogueEnd;

    public TextManager textManager;
    public WritingStyle writingStyle;
    public TMP_Text dialogueText;
    public TMP_Text nameText;
    public string defaultSpeaker = "";

    void Awake()
    {
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

        currentAsset = textAsset;
        onDialogueStart?.Invoke();
        EnterPart(textAsset.entry);
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
        currentPart?.onExit?.Invoke();
        currentPart = part;
        HideButtons();

        if (part == null)
        {
            FinishDialogue();
            return;
        }

        part.onEnter?.Invoke();

        switch (part)
        {
            case SimpleText simple:
                ShowText(simple);
                break;

            case ConditionTable table:
                OnConditionTableEnter(table);
                break;

            case EntryPart entry:
                EnterPart(entry.next);
                break;

            default:
                Debug.LogWarning("unsupported part type " + part.GetType().Name);
                EnterPart(null);
                break;
        }
    }

    void Advance()
    {
        if (currentPart is SimpleText simple) EnterPart(simple.next);
    }

    void FinishDialogue()
    {
        currentAsset = null;
        finishedWriting = false;
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

        string line = part.line;

        if (string.IsNullOrWhiteSpace(line))
        {
            if (dialogueText != null) dialogueText.text = string.Empty;
            finishedWriting = true;
            return;
        }

        if (textManager == null || writingStyle == null || dialogueText == null)
        {
            if (dialogueText != null) dialogueText.text = line;
            finishedWriting = true;
            return;
        }

        textManager.Write(line, dialogueText, writingStyle);
        finishedWriting = false;
    }

    void OnWritingEnded()
    {
        finishedWriting = true;
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

    public void OnConditionTableEnter(ConditionTable part)
    {
        HideButtons();

        int index = 0;
        foreach (ConditionEntry entry in part.entries)
        {
            if (index >= buttons.Count)
            {
                Debug.LogWarning("not enough option buttons for this condition table");
                break;
            }

            Button button = buttons[index];
            ConditionEntry chosen = entry;

            button.gameObject.SetActive(true);
            button.onClick.AddListener(() => OnOptionChosen(chosen));
            SetButtonLabel(button, entry, index);

            index++;
        }

        if (index == 0) EnterPart(null);
    }

    void OnOptionChosen(ConditionEntry entry)
    {
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
            textManager.Skip();
            return;
        }

        Advance();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (dialogueText != null)
        {
            int link = TMP_TextUtilities.FindIntersectingLink(dialogueText, eventData.position, eventData.pressEventCamera);
            if (link != -1) return;
        }

        OnTextClick();
    }

#endregion

}