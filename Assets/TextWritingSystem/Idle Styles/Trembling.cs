using UnityEngine;

[CreateAssetMenu(menuName = "WritingStyles/Trembling")]
public class Trembling : IdleStyle
{
    [Min(0f)] public float minDistance = 0f;
    [Min(0f)] public float maxDistance = 2f;
    public int seed;

    public override void ApplyIdle(TextRenderState state, TextElement element, float time)
    {
        float duration = Mathf.Max(elementDuration, 0.0001f);
        float cycleTime = time / duration;
        int cycle = Mathf.FloorToInt(cycleTime);
        float phase = cycleTime - cycle;

        float angle = Random01(element.start, cycle, 0) * Mathf.PI * 2f;
        float distance = Mathf.Lerp(minDistance, maxDistance, Random01(element.start, cycle, 1));
        float reach = Mathf.Sin(phase * Mathf.PI) * distance;

        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * reach;
        state.Set(element, 1f, offset, 1f);
    }

    float Random01(int elementStart, int cycle, int channel)
    {
        unchecked
        {
            uint h = (uint)seed * 2654435761u;
            h ^= (uint)elementStart * 73856093u;
            h ^= (uint)cycle * 19349663u;
            h ^= (uint)channel * 83492791u;
            h ^= h >> 15;
            h *= 2246822519u;
            h ^= h >> 13;
            h *= 3266489917u;
            h ^= h >> 16;
            return (h & 0xFFFFFFu) / (float)0x1000000;
        }
    }
}