using UnityEngine;

[CreateAssetMenu(menuName = "WritingStyles/Typing")]
public class Typing : WritingStyle
{
    public AudioClip typeSound;
    [Min(1)] public int everyNthElement = 1;

    public override void OnElementStart(TextElement element, int index, bool erasing)
    {
        if (erasing || index % Mathf.Max(everyNthElement, 1) != 0) return;
        PlaySound(typeSound);
    }

    public override void Apply(TextRenderState state, TextElement element, float t, bool erasing)
    {
        bool started = t >= 0f;
        float alpha = erasing ? (started ? 0f : 1f) : (started ? 1f : 0f);
        state.Set(element, alpha, Vector3.zero, 1f);
    }
}