using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class TextLinkRelay : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
{
    [HideInInspector] public TextManager owner;

    TMP_Text text;

    TMP_Text Text
    {
        get
        {
            if (text == null) text = GetComponent<TMP_Text>();
            return text;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        bool left = eventData.button == PointerEventData.InputButton.Left;
        if (left && owner != null && owner.HandleClick(Text, eventData)) return;

        Transform parent = transform.parent;
        if (parent == null) return;

        ExecuteEvents.ExecuteHierarchy(parent.gameObject, eventData, ExecuteEvents.pointerClickHandler);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (owner != null) owner.HandleMove(Text, eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner != null) owner.HandleExit(Text);
    }
}
