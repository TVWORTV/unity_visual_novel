using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Writer : MonoBehaviour
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
    Coroutine idleRoutine;
    bool currentErasing;
    bool skipRequested;
    readonly TextRenderState renderState = new TextRenderState();

#region public

    public bool IsBusy => current != null;
    public bool IsIdling => idleRoutine != null;
    public string TargetMessage => targetMessage;

    public void Write(string message, TMP_Text writeOn, WritingStyle style, IdleStyle idle = null)
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

        if (idle == null) idle = style as IdleStyle;

        current = StartCoroutine(Run(writeOn, style, false, idle));
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

        current = StartCoroutine(Run(eraseOn, toUse, true, null));
    }

    public void Idle(TMP_Text target, IdleStyle style)
    {
        if (target == null || style == null)
        {
            Debug.LogError("idle needs a text and a style");
            return;
        }

        if (current != null)
        {
            Debug.LogWarning("cannot idle while writing or erasing");
            return;
        }

        StopIdle();
        Bind(target);
        StartIdle(target, style);
    }

    public void StopIdle()
    {
        if (idleRoutine == null) return;

        StopCoroutine(idleRoutine);
        idleRoutine = null;

        if (text != null) text.ForceMeshUpdate(true, true);
    }

    public void Skip()
    {
        if (current != null) skipRequested = true;
    }

#endregion

#region private

    void Bind(TMP_Text target)
    {
        text = target;
        hoveredLinkIndex = -1;
    }

    void CancelCurrent()
    {
        StopIdle();

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

    void StartIdle(TMP_Text target, IdleStyle style)
    {
        target.ForceMeshUpdate(true, true);
        idleRoutine = StartCoroutine(RunIdle(target, style));
    }

    IEnumerator RunIdle(TMP_Text target, IdleStyle style)
    {
        renderState.Capture(target);

        List<TextElement> elements = BuildElements(target.textInfo, style.elementType);
        if (style.randomChars) Shuffle(elements);

        float delay = Mathf.Max(style.delayBetweenElements, 0f);
        float elapsed = 0f;

        while (true)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                style.ApplyIdle(renderState, elements[i], Mathf.Max(elapsed - i * delay, 0f));
            }

            renderState.Flush();

            yield return null;
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }
    }

    IEnumerator Run(TMP_Text target, WritingStyle style, bool erasing, IdleStyle idle)
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

        while (elapsed < total && !skipRequested)
        {
            ApplyFrame(elements, style, erasing, delay, duration, elapsed);
            renderState.Flush();

            yield return null;
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        ApplyFrame(elements, style, erasing, delay, duration, float.MaxValue);
        renderState.Flush();

        if (erasing)
        {
            target.text = string.Empty;
            targetMessage = "";
            hoveredLinkIndex = -1;
        }

        current = null;
        skipRequested = false;

        if (!erasing && idle != null) StartIdle(target, idle);

        RaiseEnd(erasing);
    }

    void ApplyFrame(List<TextElement> elements, WritingStyle style, bool erasing, float delay, float duration, float elapsed)
    {
        for (int i = 0; i < elements.Count; i++)
        {
            float local = elapsed - i * delay;
            float t = local < 0f ? -1f : Mathf.Clamp01(local / duration);
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

}