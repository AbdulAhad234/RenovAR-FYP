using UnityEngine;
using UnityEngine.InputSystem;

public class FreeFlyCamera : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float fastMoveMultiplier = 3f;
    public float mouseSensitivity = 0.1f;
    public float verticalSpeed = 3f;

    private float rotationX;
    private float rotationY;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 angles = transform.eulerAngles;
        rotationX = angles.y;
        rotationY = angles.x;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleCursorToggle();
    }

    void HandleMouseLook()
    {
        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        rotationX += mouseDelta.x * mouseSensitivity;
        rotationY -= mouseDelta.y * mouseSensitivity;

        rotationY = Mathf.Clamp(rotationY, -90f, 90f);

        transform.rotation = Quaternion.Euler(rotationY, rotationX, 0f);
    }

    void HandleMovement()
    {
        if (Keyboard.current == null) return;

        float speed = moveSpeed;

        if (Keyboard.current.leftShiftKey.isPressed)
            speed *= fastMoveMultiplier;

        Vector3 direction = Vector3.zero;

        if (Keyboard.current.wKey.isPressed)
            direction += transform.forward;

        if (Keyboard.current.sKey.isPressed)
            direction -= transform.forward;

        if (Keyboard.current.aKey.isPressed)
            direction -= transform.right;

        if (Keyboard.current.dKey.isPressed)
            direction += transform.right;

        if (Keyboard.current.eKey.isPressed)
            direction += Vector3.up;

        if (Keyboard.current.qKey.isPressed)
            direction -= Vector3.up;

        transform.position += direction * speed * Time.deltaTime;
    }

    void HandleCursorToggle()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
