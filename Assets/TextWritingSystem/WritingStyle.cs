using UnityEngine;

public enum WritingStyleElement
{
    Character
    , Word
    , Sentence
    , WholeText
}

public readonly struct TextElement
{
    public readonly int start;
    public readonly int count;

    public TextElement(int start, int count)
    {
        this.start = start;
        this.count = count;
    }
}

public abstract class WritingStyle : ScriptableObject
{
    public bool randomChars;
    public WritingStyleElement elementType;
    [Min(0f)] public float delayBetweenElements = 0.05f;
    [Min(0.0001f)] public float elementDuration = 0.2f;
    public bool reverseOnErase = true;

    public abstract void Apply(TextRenderState state, TextElement element, float t, bool erasing);

    public virtual void OnElementStart(TextElement element, int index, bool erasing)
    {
    }

    protected void PlaySound(AudioClip clip)
    {
        if (clip == null || SFXManager.Instance == null) return;
        SFXManager.Instance.Play(clip);
    }
}