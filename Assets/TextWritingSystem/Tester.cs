using TMPro;
using UnityEngine;

public class TextManagerTester : MonoBehaviour
{
    public TextManager manager;
    public TMP_Text target;
    public WritingStyle style;
    [TextArea(2, 6)] public string message = "Hello there. This is a <link=\"test\">clickable link</link>! Does it work?";

    void OnEnable()
    {
        if (manager == null) return;
        manager.onLinkClicked += LogClick;
        manager.onLinkHoverEnter += LogEnter;
        manager.onLinkHoverExit += LogExit;
    }

    void OnDisable()
    {
        if (manager == null) return;
        manager.onLinkClicked -= LogClick;
        manager.onLinkHoverEnter -= LogEnter;
        manager.onLinkHoverExit -= LogExit;
    }

    void LogClick(string id)
    {
        Debug.Log("link clicked: " + id);
    }

    void LogEnter(string id)
    {
        Debug.Log("link hover enter: " + id);
    }

    void LogExit(string id)
    {
        Debug.Log("link hover exit: " + id);
    }

    [ContextMenu("Write")]
    public void Write()
    {
        manager.Write(message, target, style);
    }

    [ContextMenu("Erase")]
    public void Erase()
    {
        manager.Erase(target, style);
    }

    [ContextMenu("Skip")]
    public void Skip()
    {
        manager.Skip();
    }

    [ContextMenu("Write Then Erase")]
    public void WriteThenErase()
    {
        manager.onWritingEnd += EraseAfterWrite;
        Write();
    }

    void EraseAfterWrite()
    {
        manager.onWritingEnd -= EraseAfterWrite;
        Erase();
    }
}