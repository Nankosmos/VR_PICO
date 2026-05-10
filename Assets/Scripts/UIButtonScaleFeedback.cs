using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonScaleFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform target;
    public float selectedScale = 1.08f;
    public float pressedScale = 0.98f;
    public float scaleSpeed = 12f;

    private Vector3 normalScale;
    private Vector3 targetScale;
    private bool isSelectedOrHovered;

    void Awake()
    {
        if (target == null)
        {
            target = transform as RectTransform;
        }

        normalScale = target != null ? target.localScale : Vector3.one;
        targetScale = normalScale;
    }

    void OnDisable()
    {
        isSelectedOrHovered = false;

        if (target != null)
        {
            target.localScale = normalScale;
            targetScale = normalScale;
        }
    }

    void Update()
    {
        if (target == null) return;

        target.localScale = Vector3.Lerp(target.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isSelectedOrHovered = true;
        SetSelectedScale();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isSelectedOrHovered = false;
        SetNormalScale();
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelectedOrHovered = true;
        SetSelectedScale();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelectedOrHovered = false;
        SetNormalScale();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetPressedScale();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isSelectedOrHovered)
        {
            SetSelectedScale();
        }
        else
        {
            SetNormalScale();
        }
    }

    void SetNormalScale()
    {
        targetScale = normalScale;
    }

    void SetSelectedScale()
    {
        targetScale = normalScale * selectedScale;
    }

    void SetPressedScale()
    {
        targetScale = normalScale * pressedScale;
    }
}
