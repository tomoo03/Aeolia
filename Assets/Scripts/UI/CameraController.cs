using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CameraController : MonoBehaviour
{
    public Camera mainCamera;
    public float horizontalRotationSpeed = 3f;
    public float verticalRotationSpeed = 3f;

    private bool isRotating = false;
    private Vector2 touchStartPos;

    void Update()
    {
        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        else
        {
            HandleMouseInput();
        }
    }

    private void HandleTouchInput()
    {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began && !IsPointerOverUIObject(touch.position))
        {
            isRotating = true;
            touchStartPos = touch.position;
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            isRotating = false;
        }

        if (isRotating && touch.phase == TouchPhase.Moved)
        {
            RotateCamera(touch.position - touchStartPos);
            touchStartPos = touch.position;
        }
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUIObject(Input.mousePosition))
        {
            isRotating = true;
            touchStartPos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isRotating = false;
        }

        if (isRotating && Input.GetMouseButton(0))
        {
            Vector2 mouseDelta = (Vector2)Input.mousePosition - touchStartPos;
            RotateCamera(mouseDelta);
            touchStartPos = Input.mousePosition;
        }
    }

    private void RotateCamera(Vector2 delta)
    {
        float xRotation = delta.y * verticalRotationSpeed * Time.deltaTime;
        float yRotation = -delta.x * horizontalRotationSpeed * Time.deltaTime;

        mainCamera.transform.eulerAngles += new Vector3(xRotation, yRotation, 0);
    }

    private bool IsPointerOverUIObject(Vector2 screenPosition)
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = screenPosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }
}