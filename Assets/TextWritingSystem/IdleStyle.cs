using UnityEngine;

public abstract class IdleStyle : WritingStyle
{
    public override void Apply(TextRenderState state, TextElement element, float t, bool erasing)
    {
        bool started = t >= 0f;
        float alpha = erasing ? (started ? 0f : 1f) : (started ? 1f : 0f);
        state.Set(element, alpha, Vector3.zero, 1f);
    }

    public abstract void ApplyIdle(TextRenderState state, TextElement element, float time);
}