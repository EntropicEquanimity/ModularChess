using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_Text))]
public class TMPLinkHandler : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text m_TextMeshPro;
    private Canvas m_Canvas;
    private Camera m_Camera;

    void Awake()
    {
        m_TextMeshPro = GetComponent<TMP_Text>();
        m_Canvas = GetComponentInParent<Canvas>();
        if (m_Canvas != null && m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay) { m_Camera = null; }
        else { m_Camera = m_Canvas.worldCamera != null ? m_Canvas.worldCamera : Camera.main; }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextMeshPro, eventData.position, m_Camera);
        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = m_TextMeshPro.textInfo.linkInfo[linkIndex];
            string linkId = linkInfo.GetLinkID();
            if (linkId.StartsWith("http://") || linkId.StartsWith("https://")) { Application.OpenURL(linkId); }
        }
    }
}