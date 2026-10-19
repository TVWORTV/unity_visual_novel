
using TMPro;
using UnityEngine;

public class TextRenderState
{
    TMP_Text text;
    Vector3[][] sourceVertices;
    Color32[][] sourceColors;

    public void Capture(TMP_Text target)
    {
        text = target;
        TMP_MeshInfo[] meshInfo = target.textInfo.meshInfo;
        sourceVertices = new Vector3[meshInfo.Length][];
        sourceColors = new Color32[meshInfo.Length][];

        for (int m = 0; m < meshInfo.Length; m++)
        {
            if (meshInfo[m].vertices == null) continue;
            sourceVertices[m] = (Vector3[])meshInfo[m].vertices.Clone();
            sourceColors[m] = (Color32[])meshInfo[m].colors32.Clone();
        }
    }

    public void Set(TextElement element, float alpha, Vector3 offset, float scale)
    {
        TMP_TextInfo info = text.textInfo;
        float clampedAlpha = Mathf.Clamp01(alpha);
        int end = element.start + element.count;

        for (int i = element.start; i < end; i++)
        {
            TMP_CharacterInfo character = info.characterInfo[i];
            if (!character.isVisible) continue;

            int mesh = character.materialReferenceIndex;
            int v = character.vertexIndex;

            Vector3[] sourceV = sourceVertices[mesh];
            Color32[] sourceC = sourceColors[mesh];
            Vector3[] targetV = info.meshInfo[mesh].vertices;
            Color32[] targetC = info.meshInfo[mesh].colors32;

            Vector3 center = (sourceV[v] + sourceV[v + 2]) * 0.5f;

            for (int k = 0; k < 4; k++)
            {
                targetV[v + k] = center + (sourceV[v + k] - center) * scale + offset;

                Color32 color = sourceC[v + k];
                color.a = (byte)(color.a * clampedAlpha);
                targetC[v + k] = color;
            }
        }
    }

    public void Flush()
    {
        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }
}