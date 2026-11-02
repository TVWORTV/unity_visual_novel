using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LinkHandler : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
{

#region Pointer 

    bool IsValidLink(int index)
    {
        return text != null && index >= 0 && index < text.textInfo.linkCount;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (text == null) return;

        int index = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, uiCamera);
        if (index == -1) return;

        TMP_LinkInfo info = text.textInfo.linkInfo[index];
        HandleLinkClicked(info.GetLinkID());
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (text == null) return;

        int index = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, uiCamera);

        if (index == hoveredLinkIndex) return;

        if (IsValidLink(hoveredLinkIndex))
        {
            OnLinkHoverExit(text.textInfo.linkInfo[hoveredLinkIndex].GetLinkID());
        }

        hoveredLinkIndex = index;

        if (IsValidLink(hoveredLinkIndex))
        {
            OnLinkHoverEnter(text.textInfo.linkInfo[hoveredLinkIndex].GetLinkID());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoveredLinkIndex == -1) return;

        if (IsValidLink(hoveredLinkIndex))
        {
            OnLinkHoverExit(text.textInfo.linkInfo[hoveredLinkIndex].GetLinkID());
        }

        hoveredLinkIndex = -1;
    }

#endregion

#region Links 

    void HandleLinkClicked(string linkId)
    {
        onLinkClicked?.Invoke(linkId);
    }

    void OnLinkHoverEnter(string linkId)
    {
        onLinkHoverEnter?.Invoke(linkId);
    }

    void OnLinkHoverExit(string linkId)
    {
        onLinkHoverExit?.Invoke(linkId);
    }

#endregion

}