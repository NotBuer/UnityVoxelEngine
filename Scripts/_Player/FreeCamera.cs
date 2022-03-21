using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeCamera : MonoBehaviour {

    private const string MOUSE_X = "Mouse X";
    private const string MOUSE_Y = "Mouse Y";

    [Header("Camera Speed")]
    [SerializeField] private float cameraSpeed;

    [Header("Mouse Speed")]
    [SerializeField] [Range(1, 200)] private float mouseHorizontalSpeed;
    [SerializeField] [Range(1, 200)] private float mouseVerticalSpeed;

    private float mouseX = 0f;
    private float mouseY = 0;

    private Camera thisCamera;

    private void Awake() {
        thisCamera = GetComponent<Camera>();
    }

    private void Start() {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void Update() {
        if (Input.GetKey(KeyCode.LeftControl)) {
            Cursor.visible = true;
            return;
        }

        Cursor.visible = false;
        CameraMovement();
        CameraRotation();
    }

    private void CameraMovement() {
        Vector3 movement = new Vector3();
        float camSpeed = cameraSpeed;

        // Move forward.
        if (Input.GetKey(KeyCode.W)) {
            movement += transform.rotation * Vector3.forward;
        }
        // Move backward.
        else if (Input.GetKey(KeyCode.S)) {
            movement += transform.rotation * Vector3.back;
        }

        // Move left.
        if (Input.GetKey(KeyCode.A)) {
            movement += transform.rotation * Vector3.left;
        }
        // Move right.
        else if (Input.GetKey(KeyCode.D)) {
            movement += transform.rotation * Vector3.right;
        }

        // Apply extra camera speed;
        if (Input.GetKey(KeyCode.LeftShift)) {
            camSpeed *= 2f;
        }

        transform.Translate(movement * (camSpeed * Time.deltaTime), Space.World);
    }

    private void CameraRotation() {
        // Get the mouse input.
        mouseX += Input.GetAxis(MOUSE_X) * mouseHorizontalSpeed * Time.deltaTime;
        mouseY -= Input.GetAxis(MOUSE_Y) * mouseVerticalSpeed * Time.deltaTime;

        // Lock the mouse Y in the range of total 180 degrees.
        mouseY = Mathf.Clamp(mouseY, -90, 90);

        // Apply the mouse Y to camera rotation X axis, and apply the mouseX to the camera rotation Y axis.
        transform.eulerAngles = new Vector3(mouseY, mouseX);
    }

}
