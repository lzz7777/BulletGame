using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;

public class UIDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private RectTransform canvasRt;
    private Vector2 offset;
    private Vector2 startPosition;
    [BoxGroup("Dock设置"), LabelText("折叠按钮"), Tooltip("拖入一个按钮，点击可折叠/展开当前节点")]
    public Button dockButton;
    [BoxGroup("Dock设置"), LabelText("拖拽边距"), Tooltip("上下左右边界安全距离，拖拽时不越界")]
    public float margin = 100f;
    [BoxGroup("Dock设置"), LabelText("隐藏出屏幕距离"), Tooltip("折叠后节点离屏幕边缘的偏移像素，按钮仍在屏内")]
    public float btnmargin = 0f;
    [BoxGroup("Dock设置"), LabelText("按钮左右侧显示自适应"), Tooltip("关闭则固定向右折叠，按钮显示左侧")]
    public bool autoPlaceButton = true;
    [BoxGroup("Dock设置"), LabelText("按钮间距"), Tooltip("按钮与节点边缘的本地偏移距离")]
    public float buttonGap = 10f;
    [BoxGroup("Dock设置"), LabelText("跟随尺寸变化"), Tooltip("当节点尺寸(sizeDelta)变化时，自动重摆按钮位置")]
    public bool adaptToSizeDelta = false;
    private bool isCollapsed;
    private bool preferLeft;
    private Vector3 lastExpandedPos;
    private bool initialized;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasRt = canvas.GetComponent<RectTransform>();
        lastExpandedPos = rectTransform.localPosition;
        if (dockButton != null) dockButton.onClick.AddListener(ToggleDock);
        if (autoPlaceButton) PlaceDockButton();
        initialized = true;
    }

    void OnDestroy()
    {
        if (dockButton != null) dockButton.onClick.RemoveListener(ToggleDock);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPosition = rectTransform.localPosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position,
            eventData.pressEventCamera, out Vector2 localPoint);
        offset = localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 newPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform,
                eventData.position, eventData.pressEventCamera, out newPos))
        {
            float w = rectTransform.rect.width;
            float h = rectTransform.rect.height;
            var pivot = rectTransform.pivot;
            float halfCanvasW = canvasRt.rect.width / 2f;
            float halfCanvasH = canvasRt.rect.height / 2f;
            float leftExtent = pivot.x * w;
            float rightExtent = (1f - pivot.x) * w;
            float bottomExtent = pivot.y * h;
            float topExtent = (1f - pivot.y) * h;
            float minX = -halfCanvasW - margin + leftExtent;    // X 可以藏100px
            float maxX = halfCanvasW + margin - rightExtent;    // X 可以藏100px
            float minY = -halfCanvasH + margin + bottomExtent;  // Y 需要距边100px
            float maxY = halfCanvasH - margin - topExtent;      // Y 需要距边100px

            var pos = startPosition + newPos - offset;

            var clamped = pos;
            clamped.x = Mathf.Clamp(clamped.x, Mathf.Min(minX, maxX), Mathf.Max(minX, maxX));
            clamped.y = Mathf.Clamp(clamped.y, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));
            if (clamped != pos) { startPosition = clamped; offset = newPos; }

            rectTransform.localPosition = clamped;
            preferLeft = clamped.x <= 0f;
            isCollapsed = false;
            lastExpandedPos = clamped;
            if (autoPlaceButton) PlaceDockButton();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("拖拽结束");
    }

    private void ToggleDock()
    {
        if (isCollapsed) ExpandToMargin();
        else CollapseToEdge();
    }

    private void CollapseToEdge()
    {
        var buttonRt = dockButton != null ? dockButton.GetComponent<RectTransform>() : null;
        float w = rectTransform.rect.width;
        float h = rectTransform.rect.height;
        var pivot = rectTransform.pivot;
        float halfCanvasW = canvasRt.rect.width / 2f;
        float halfCanvasH = canvasRt.rect.height / 2f;
        float leftExtent = pivot.x * w;
        float rightExtent = (1f - pivot.x) * w;
        float bottomExtent = pivot.y * h;
        float topExtent = (1f - pivot.y) * h;
        float minY = -halfCanvasH + margin + bottomExtent;
        float maxY = halfCanvasH - margin - topExtent;

        preferLeft = rectTransform.localPosition.x <= 0f;
        if (!autoPlaceButton) preferLeft = false;
        float targetX;
        if (buttonRt != null)
        {
            float bw = buttonRt.rect.width;
            var bp = buttonRt.pivot;
            float bLeftExtent = bp.x * bw;
            float bRightExtent = (1f - bp.x) * bw;
            float targetButtonX = preferLeft
                ? -halfCanvasW + bLeftExtent + btnmargin
                : halfCanvasW  - bRightExtent - btnmargin;
            targetX = targetButtonX - buttonRt.localPosition.x;
        }
        else
        {
            targetX = preferLeft
                ? -halfCanvasW + leftExtent + btnmargin
                : halfCanvasW - rightExtent - btnmargin;
        }
        float targetY = Mathf.Clamp(rectTransform.localPosition.y, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));
        var target = new Vector3(targetX, targetY, rectTransform.localPosition.z);
        rectTransform.DOLocalMove(target, 0.25f).SetEase(Ease.OutCubic);
        startPosition = target;
        isCollapsed = true;
        if (autoPlaceButton) PlaceDockButton();
    }

    private void ExpandToMargin()
    {
        float w = rectTransform.rect.width;
        float h = rectTransform.rect.height;
        var pivot = rectTransform.pivot;
        float halfCanvasW = canvasRt.rect.width / 2f;
        float halfCanvasH = canvasRt.rect.height / 2f;
        float leftExtent = pivot.x * w;
        float rightExtent = (1f - pivot.x) * w;
        float bottomExtent = pivot.y * h;
        float topExtent = (1f - pivot.y) * h;
        float minY = -halfCanvasH + margin + bottomExtent;
        float maxY = halfCanvasH - margin - topExtent;

        var target = lastExpandedPos;
        if (target == Vector3.zero)
        {
            float targetX = preferLeft
                ? -halfCanvasW + margin + leftExtent
                : halfCanvasW - margin - rightExtent;
            float targetY = Mathf.Clamp(rectTransform.localPosition.y, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));
            target = new Vector3(targetX, targetY, rectTransform.localPosition.z);
        }
        target.y = Mathf.Clamp(target.y, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));
        rectTransform.DOLocalMove(target, 0.25f).SetEase(Ease.OutCubic);
        startPosition = target;
        isCollapsed = false;
        if (autoPlaceButton) PlaceDockButton();
    }

    private void PlaceDockButton()
    {
        if (dockButton == null) return;
        var buttonRt = dockButton.GetComponent<RectTransform>();
        if (buttonRt == null || rectTransform == null) return;
        float w = rectTransform.rect.width;
        float h = rectTransform.rect.height;
        var pivot = rectTransform.pivot;
        float leftEdgeLocal = -pivot.x * w;
        float rightEdgeLocal = (1f - pivot.x) * w;
        // 将按钮放在可见侧：节点在左则按钮在右，节点在右则按钮在左
        float x = preferLeft
            ? rightEdgeLocal + buttonGap
            : leftEdgeLocal - buttonGap;
        // y 保持按钮当前本地 y，以免影响垂直布局
        var bp = buttonRt.localPosition;
        bp.x = x;
        buttonRt.localPosition = bp;
    }

    void OnRectTransformDimensionsChange()
    {
        if (!initialized) return;
        if (autoPlaceButton && adaptToSizeDelta)
        {
            PlaceDockButton();
        }
    }
}
