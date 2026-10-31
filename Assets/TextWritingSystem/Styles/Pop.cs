
using UnityEngine;

[CreateAssetMenu(menuName = "WritingStyles/Pop")]
public class Pop : WritingStyle
{
    [Min(0f)] public float explodeAmount = 0.4f;
    [Range(0f, 1f)] public float shrinkAmount = 0.5f;
    public AudioClip popSound;
    public bool playOnErase;

    public override void OnElementStart(TextElement element, int index, bool erasing)
    {
        if (erasing && !playOnErase) return;
        PlaySound(popSound);
    }

    public override void Apply(TextRenderState state, TextElement element, float t, bool erasing)
    {
        bool started = t >= 0f;
        float p = Mathf.Max(t, 0f);
        float peak = 1f + explodeAmount;
        float low = 1f - shrinkAmount;

        if (!erasing)
        {
            if (!started)
            {
                state.Set(element, 0f, Vector3.zero, low);
                return;
            }

            float scale = p < 0.5f
                ? Mathf.Lerp(low, peak, Mathf.SmoothStep(0f, 1f, p * 2f))
                : Mathf.Lerp(peak, 1f, Mathf.SmoothStep(0f, 1f, (p - 0.5f) * 2f));

            state.Set(element, Mathf.Clamp01(p * 4f), Vector3.zero, scale);
            return;
        }

        if (!started)
        {
            state.Set(element, 1f, Vector3.zero, 1f);
            return;
        }

        float eraseScale = p < 0.5f
            ? Mathf.Lerp(1f, peak, Mathf.SmoothStep(0f, 1f, p * 2f))
            : Mathf.Lerp(peak, low, Mathf.SmoothStep(0f, 1f, (p - 0.5f) * 2f));

        float eraseAlpha = p < 0.5f ? 1f : 1f - (p - 0.5f) * 2f;
        state.Set(element, eraseAlpha, Vector3.zero, eraseScale);
    }
}