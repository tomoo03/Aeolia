using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Action<PointerEventData> OnBeginDragEvent;
    public Action<PointerEventData> OnDragEvent;
    public Action<PointerEventData> OnEndDragEvent;
    private Canvas canvas;

    // ドラッグ開始
    public void OnBeginDrag(PointerEventData eventData) {
        OnBeginDragEvent?.Invoke(eventData);
    }

    // ドラッグ中
    public void OnDrag(PointerEventData eventData) {
        OnDragEvent?.Invoke(eventData);
    }

    // ドラッグ終了
    public void OnEndDrag(PointerEventData eventData) {
        OnEndDragEvent?.Invoke(eventData);
    }

    /// <summary>
    /// スクリーン座標をcanvas上の座標に変換する
    /// </summary>
    /// <param name="pointerPos">タッチしたスクリーン座標</param>
    /// <returns>canvas上の座標</returns>
    public Vector2 ConvertScreenPositionToCanvasPosition(Vector2 pointerPos) {
        if (canvas == null) {
            canvas = FindParentCanvas(transform);
        }
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, pointerPos, canvas.worldCamera, out Vector2 localPointerPos);
        return localPointerPos;
    }

    /// <summary>
    /// 所属するCanvasを取得
    /// </summary>
    private Canvas FindParentCanvas(Transform transform) {
        if (transform == null) {
            return null;
        }

        var canvas = transform.GetComponent<Canvas>();
        return canvas != null ? canvas : FindParentCanvas(transform.parent);
    }
}
