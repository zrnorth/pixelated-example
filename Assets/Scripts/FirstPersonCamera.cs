using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FirstPersonCamera : MonoBehaviour
{
    [SerializeField]
    private float _speed = 5.0f;
    [SerializeField]
    private float _sensitivity = 3.0f;
    [SerializeField]
    private bool _inverted = false;

    private Vector2 _mouseLook;

    private void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            Cursor.lockState = CursorLockMode.None;
        }
        HandleMovement();
        HandleMouseLook();
    }

    void HandleMovement() {
        // We should just use an axis here, but this is a substitute for portability
        float yAxisInput = 0f;
        if (Input.GetKey(KeyCode.E)) {
            yAxisInput = 1f;
        } else if (Input.GetKey(KeyCode.Q)) {
            yAxisInput = -1f;
        }
        Vector3 normalizedMoveDelta = (new Vector3(Input.GetAxisRaw("Horizontal"), yAxisInput, Input.GetAxisRaw("Vertical"))).normalized;
        transform.Translate(normalizedMoveDelta * _speed * Time.deltaTime);
    }

    void HandleMouseLook() {
        Vector2 mouseDelta = _sensitivity * new Vector2(
                    Input.GetAxisRaw("Mouse X"),
                    Input.GetAxisRaw("Mouse Y")
                );
        if (_inverted) {
            mouseDelta.y *= -1;
        }

        _mouseLook = new Vector2(
            _mouseLook.x + mouseDelta.x,
            Mathf.Clamp(_mouseLook.y + mouseDelta.y, -90f, 90f)
        );

        // Rotate to the new look direction
        transform.localRotation = Quaternion.Euler(-_mouseLook.y, _mouseLook.x, 0);
    }
}
