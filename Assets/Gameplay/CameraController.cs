using System.Collections;
using UnityEngine;

/// <summary>
/// A simple free camera to be added to a Unity game object.
/// 
/// Keys:
///	wasd / arrows	- movement
///	q/e 			- up/down (local space)
///	r/f 			- up/down (world space)
///	pageup/pagedown	- up/down (world space)
///	hold shift		- enable fast movement mode
///	right mouse  	- enable free look
///	mouse			- free look / rotation
///     
/// </summary>
public class CameraController : MonoBehaviourSingleton<CameraController>
{
    [Component] private Camera cam;

    public bool controlsEnabled;

    /// <summary>
    /// Normal speed of camera movement.
    /// </summary>
    public float movementSpeed = 10f;

    /// <summary>
    /// Speed of camera movement when shift is held down,
    /// </summary>
    public float fastMovementSpeed = 50f;

    /// <summary>
    /// Sensitivity for free look.
    /// </summary>
    public float freeLookSensitivity = 3f;

    /// <summary>
    /// Amount to zoom the camera when using the mouse wheel.
    /// </summary>
    public float zoomSensitivity = 10f;

    /// <summary>
    /// Amount to zoom the camera when using the mouse wheel (fast mode).
    /// </summary>
    public float fastZoomSensitivity = 50f;

    public float dragSpeed = 1f;
    public float fastDragSpeed = 5f;


    public GameObject d4;
    public GameObject light;

    /// <summary>
    /// Set to true when free looking (on right mouse button).
    /// </summary>
    private bool looking = false;

    private Vector3 pivotPoint;

    private bool _upsideDown;
    public bool UpsideDown
    {
        get => _upsideDown;
        set
        {
            _upsideDown = value;
            if (value)
            {
                transform.eulerAngles = transform.eulerAngles.Z(180);
            }
            else
            {
                transform.eulerAngles = transform.eulerAngles.Z(0);
            }
        }
    }

    private void Update()
    {
        if (!controlsEnabled)
            return;

        var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var movementSpeed = fastMode ? this.fastMovementSpeed : this.movementSpeed;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            transform.position = transform.position + (-transform.right * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            transform.position = transform.position + (transform.right * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            transform.position = transform.position + (transform.forward * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            transform.position = transform.position + (-transform.forward * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.Q))
        {
            transform.position = transform.position + (transform.up * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.E))
        {
            transform.position = transform.position + (-transform.up * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.Mouse4) || Input.GetKey(KeyCode.PageUp))
        {
            transform.position = transform.position + (Vector3.up * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.Mouse3) || Input.GetKey(KeyCode.PageDown))
        {
            transform.position = transform.position + (-Vector3.up * movementSpeed * Time.deltaTime);
        }

        // Only work with the Left Alt pressed
        if (Input.GetKey(KeyCode.LeftAlt))
        {
            //drag camera around with Middle Mouse
            if (Input.GetMouseButton(2))
            {
                var dragSpeed = fastMode ? this.fastDragSpeed : this.dragSpeed;
                transform.Translate(-Input.GetAxisRaw("Mouse X") * dragSpeed, -Input.GetAxisRaw("Mouse Y") * dragSpeed, 0);
            }

            if (Input.GetMouseButton(1))
            {
                //Zoom in and out with Right Mouse
                var zoomSensitivity = fastMode ? this.fastZoomSensitivity : this.zoomSensitivity;
                this.transform.Translate(0, 0, Input.GetAxisRaw("Mouse X") * zoomSensitivity * .07f, Space.Self);
            }
        }
        else
        {
            float axis = Input.GetAxis("Mouse ScrollWheel");
            if (axis != 0)
            {
                var zoomSensitivity = fastMode ? this.fastZoomSensitivity : this.zoomSensitivity;
                transform.position = transform.position + transform.forward * axis * zoomSensitivity;
            }

            if (Input.GetKey(KeyCode.Space))
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    StartLooking();
                    pivotPoint = d4.transform.position;
                }
                transform.LookAt(pivotPoint);
            }
            else
            {
                if (looking)
                {
                    float newRotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * freeLookSensitivity;
                    float newRotationY = transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * freeLookSensitivity;
                    transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
                }

                // Camera Orbit - set pivot to the clicked point
                if (Input.GetKeyDown(KeyCode.Mouse2))
                {
                    if(cam.TryGetMouseWorldPoint(out Vector3 mousePoint))
                        pivotPoint = mousePoint;
                }
            }

            // Camera Orbit
            if (Input.GetKey(KeyCode.Mouse2) || Input.GetKey(KeyCode.Space))
            {
                transform.RotateAround(pivotPoint, Vector3.up, (Input.GetAxisRaw("Mouse X")) * freeLookSensitivity);
                transform.RotateAround(pivotPoint, transform.right, -(Input.GetAxisRaw("Mouse Y") * freeLookSensitivity));
            }
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            StartLooking();
        }
        else if (Input.GetKeyUp(KeyCode.Mouse1) && !Input.GetKey(KeyCode.Space))
        {
            StopLooking();
        }
        
        if (Input.GetKeyUp(KeyCode.Space))
        {
            if(!Input.GetKey(KeyCode.Mouse1))
                StopLooking();
        }
    }

    void OnDisable()
    {
        StopLooking();
    }

    /// <summary>
    /// Enable free looking.
    /// </summary>
    public void StartLooking()
    {
        looking = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Disable free looking.
    /// </summary>
    public void StopLooking()
    {
        looking = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public bool CanSee(Vector3 point)
    {
        Vector3 viewPos = cam.WorldToViewportPoint(point);
        if (viewPos.x < 0 || viewPos.x > 1 ||
            viewPos.y < 0 || viewPos.y > 1 ||
            viewPos.z < 0)
            return false;

        return true;
    }

    public void MakeSureObjectCanBeSeen(GameObject go)
    {
        float timer = 0;
        var pevP = go.transform.position;
        var p = go.transform.position.Y(go.transform.position.y + 1.5f);
        StartCoroutine(makeSureObjectCanBeSeen());

        IEnumerator makeSureObjectCanBeSeen()
        {
            while (!CanSee(p))
            {
                timer += Time.deltaTime;
                if(timer > 3)
                {
                    go.transform.position = pevP;
                    yield break;
                }

                transform.position = transform.position + Vector3.up * 2 * Time.deltaTime; // Lift-up
                //transform.position = transform.position + transform.forward * 2 * Time.deltaTime; // Zoom-out
                yield return null;
            }
        }
    }

    public void ChangePerspective()
    {
        transform.RotateAround(d4.transform.position, Vector3.up, 180);
        light.transform.RotateAround(d4.transform.position, Vector3.up, 180);
    }
}