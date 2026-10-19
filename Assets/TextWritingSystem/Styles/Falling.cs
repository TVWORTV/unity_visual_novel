using UnityEngine;

[CreateAssetMenu(menuName = "WritingStyles/Falling")]
public class Falling : WritingStyle
{
    public bool ascendOnErase;
    public float fallDistance = 30f;

    public override void Apply(TextRenderState state, TextElement element, float t, bool erasing)
    {
        float p = Mathf.SmoothStep(0f, 1f, Mathf.Max(t, 0f));

        if (!erasing)
        {
            state.Set(element, p, new Vector3(0f, fallDistance * (1f - p), 0f), 1f);
            return;
        }

        float direction = ascendOnErase ? 1f : -1f;
        state.Set(element, 1f - p, new Vector3(0f, fallDistance * p * direction, 0f), 1f);
    }
}