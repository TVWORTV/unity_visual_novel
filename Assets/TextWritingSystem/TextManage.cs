using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TextManager : MonoBehaviour
{
    public Action onWritingStart, onWritingEnd;
    public Action onEraseStart, onEraseEnd;
    public Action<string> onLinkClicked, onLinkHoverEnter, onLinkHoverExit;

    [SerializeField] TMP_Text text;
    [SerializeField] Camera uiCamera;
    [SerializeField] bool useUnscaledTime;

    string targetMessage = "";
    WritingStyle lastStyle = null;
    int hoveredLinkIndex = -1;
    Coroutine current;
    bool currentErasing;
    bool skipRequested;
    readonly TextRenderState renderState = new TextRenderState();

    void Awake()
    {
        if (text != null) EnsureRelay(text);
    }

#region public

    public TMP_Text BoundText => text;
    public bool IsBusy => current != null;
    public string TargetMessage => targetMessage;

    public void Write(string message, TMP_Text writeOn, WritingStyle style)
    {
        if (writeOn == null || style == null)
        {
            Debug.LogError("write needs a text and a style");
            return;
        }

        CancelCurrent();

        targetMessage = message;
        lastStyle = style;
        Bind(writeOn);

        writeOn.text = message;
        writeOn.ForceMeshUpdate(true, true);

        current = StartCoroutine(Run(writeOn, style, false));
    }

    public void Erase(TMP_Text eraseOn, WritingStyle style = null)
    {
        WritingStyle toUse = style == null ? lastStyle : style;
        if (toUse == null)
        {
            Debug.LogError("no style to use");
            return;
        }

        if (eraseOn == null)
        {
            Debug.LogError("erase needs a text");
            return;
        }

        CancelCurrent();
        Bind(eraseOn);
        eraseOn.ForceMeshUpdate(true, true);

        current = StartCoroutine(Run(eraseOn, toUse, true));
    }

    public void Skip()
    {
        if (current != null) skipRequested = true;
    }

    public void Stop()
    {
        if (current == null) return;

        StopCoroutine(current);
        current = null;
        skipRequested = false;
    }

#endregion

#region private

    void Bind(TMP_Text target)
    {
        if (text != target) ReleaseHover();

        text = target;
        hoveredLinkIndex = -1;
        EnsureRelay(target);
    }

    void EnsureRelay(TMP_Text target)
    {
        target.raycastTarget = true;

        TextLinkRelay relay = target.GetComponent<TextLinkRelay>();
        if (relay == null) relay = target.gameObject.AddComponent<TextLinkRelay>();
        relay.owner = this;
    }

    void ReleaseHover()
    {
        if (IsValidLink(hoveredLinkIndex))
        {
            OnLinkHoverExit(text.textInfo.linkInfo[hoveredLinkIndex].GetLinkID());
        }

        hoveredLinkIndex = -1;
    }

    void CancelCurrent()
    {
        if (current == null) return;

        StopCoroutine(current);
        current = null;
        skipRequested = false;
        RaiseEnd(currentErasing);
    }

    void RaiseStart(bool erasing)
    {
        if (erasing) onEraseStart?.Invoke();
        else onWritingStart?.Invoke();
    }

    void RaiseEnd(bool erasing)
    {
        if (erasing) onEraseEnd?.Invoke();
        else onWritingEnd?.Invoke();
    }

    IEnumerator Run(TMP_Text target, WritingStyle style, bool erasing)
    {
        currentErasing = erasing;
        skipRequested = false;
        RaiseStart(erasing);

        renderState.Capture(target);

        List<TextElement> elements = BuildElements(target.textInfo, style.elementType);
        if (erasing && style.reverseOnErase) elements.Reverse();
        if (style.randomChars) Shuffle(elements);

        float duration = Mathf.Max(style.elementDuration, 0.0001f);
        float delay = Mathf.Max(style.delayBetweenElements, 0f);
        float total = elements.Count == 0 ? 0f : (elements.Count - 1) * delay + duration;
        float elapsed = 0f;
        bool[] started = new bool[elements.Count];

        while (elapsed < total && !skipRequested)
        {
            ApplyFrame(elements, started, style, erasing, delay, duration, elapsed, true);
            renderState.Flush();

            yield return null;
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        ApplyFrame(elements, started, style, erasing, delay, duration, float.MaxValue, !skipRequested);
        renderState.Flush();

        if (erasing)
        {
            target.text = string.Empty;
            targetMessage = "";
            hoveredLinkIndex = -1;
        }

        current = null;
        skipRequested = false;
        RaiseEnd(erasing);
    }

    void ApplyFrame(List<TextElement> elements, bool[] started, WritingStyle style, bool erasing, float delay, float duration, float elapsed, bool playSounds)
    {
        for (int i = 0; i < elements.Count; i++)
        {
            float local = elapsed - i * delay;
            float t = local < 0f ? -1f : Mathf.Clamp01(local / duration);

            if (playSounds && !started[i] && local >= 0f)
            {
                started[i] = true;
                style.OnElementStart(elements[i], i, erasing);
            }

            style.Apply(renderState, elements[i], t, erasing);
        }
    }

#endregion

#region static

    static List<TextElement> BuildElements(TMP_TextInfo info, WritingStyleElement type)
    {
        List<TextElement> elements = new List<TextElement>();
        int count = info.characterCount;

        switch (type)
        {
            case WritingStyleElement.Character:
                for (int i = 0; i < count; i++) TryAdd(elements, info, i, 1);
                break;

            case WritingStyleElement.Word:
                for (int i = 0; i < info.wordCount; i++)
                {
                    TMP_WordInfo word = info.wordInfo[i];
                    TryAdd(elements, info, word.firstCharacterIndex, word.characterCount);
                }
                break;

            case WritingStyleElement.Sentence:
                int start = 0;
                for (int i = 0; i < count; i++)
                {
                    bool terminator = IsTerminator(info.characterInfo[i].character);
                    bool nextTerminator = i + 1 < count && IsTerminator(info.characterInfo[i + 1].character);
                    if (!terminator || nextTerminator) continue;

                    TryAdd(elements, info, start, i - start + 1);
                    start = i + 1;
                }
                if (start < count) TryAdd(elements, info, start, count - start);
                break;

            case WritingStyleElement.WholeText:
                TryAdd(elements, info, 0, count);
                break;
        }

        return elements;
    }

    static bool IsTerminator(char c)
    {
        return c == '.' || c == '!' || c == '?';
    }

    static void TryAdd(List<TextElement> elements, TMP_TextInfo info, int start, int count)
    {
        if (count <= 0) return;

        for (int i = start; i < start + count; i++)
        {
            if (!info.characterInfo[i].isVisible) continue;
            elements.Add(new TextElement(start, count));
            return;
        }
    }

    static void Shuffle(List<TextElement> elements)
    {
        for (int i = elements.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (elements[i], elements[j]) = (elements[j], elements[i]);
        }
    }

#endregion

#region Pointer 

    bool IsValidLink(int index)
    {
        return text != null && index >= 0 && index < text.textInfo.linkCount;
    }

    Camera ResolveCamera()
    {
        Canvas canvas = text.canvas;
        if (canvas == null) return uiCamera;

        Canvas root = canvas.rootCanvas;
        if (root.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        if (root.worldCamera != null) return root.worldCamera;
        if (uiCamera != null) return uiCamera;
        return Camera.main;
    }

    int FindLink(PointerEventData eventData)
    {
        if (text == null) return -1;
        return TMP_TextUtilities.FindIntersectingLink(text, eventData.position, ResolveCamera());
    }

    public bool IsOverLink(PointerEventData eventData)
    {
        return FindLink(eventData) != -1;
    }

    public bool HandleClick(TMP_Text source, PointerEventData eventData)
    {
        if (source != text) return false;

        int index = FindLink(eventData);
        if (index == -1) return false;

        HandleLinkClicked(text.textInfo.linkInfo[index].GetLinkID());
        return true;
    }

    public void HandleMove(TMP_Text source, PointerEventData eventData)
    {
        if (source != text) return;

        int index = FindLink(eventData);

        if (index == hoveredLinkIndex) return;

        if (IsValidLink(hoveredLinkIndex))
        {
            OnLinkHoverExit(text.textInfo.linkInfo[hoveredLinkIndex].GetLinkID());
        }

        hoveredLinkIndex = index;

        if (IsValidLink(hoveredLinkIndex))
        {
            OnLinkHoverEnter(text.textInfo.linkInfo[hoveredLinkIndex].GetLinkID());
        }
    }

    public void HandleExit(TMP_Text source)
    {
        if (source != text) return;
        ReleaseHover();
    }

#endregion

#region Links 

    void HandleLinkClicked(string linkId)
    {
        onLinkClicked?.Invoke(linkId);
    }

    void OnLinkHoverEnter(string linkId)
    {
        onLinkHoverEnter?.Invoke(linkId);
    }

    void OnLinkHoverExit(string linkId)
    {
        onLinkHoverExit?.Invoke(linkId);
    }

#endregion

}
