
using UnityEngine;

[CreateAssetMenu(menuName = "WritingStyles/Fading")]
public class Fading : IdleStyle
{
    [Range(0f, 1f)] public float minAlpha = 0.2f;
    [Range(0f, 1f)] public float maxAlpha = 1f;

    public override void ApplyIdle(TextRenderState state, TextElement element, float time)
    {
        float phase = time / Mathf.Max(elementDuration, 0.0001f);
        float wave = 0.5f + 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, wave);
        state.Set(element, alpha, Vector3.zero, 1f);
    }
}