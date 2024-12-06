using System.Collections;
using FishNet.Object;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpHeight = 2f;
    public float gravity = -10f;
    public float slideSpeed = 10f;
    public float slideTime = 0.5f;

    [Header("Camera")]
    public float mouseSensitivity = 100f;
    public float cameraRotationLimit = 90f;

    [Header("References")]
    public Transform cameraPivot;
    public Transform headPivot;
    public GameObject[] inactiveOnLocalPlayer;
    CharacterController controller;

    Vector3 velocity;
    float xRotation = 0f;
    bool isGrounded;
    bool isSliding;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            Camera.main.transform.parent = cameraPivot;
            Camera.main.transform.localPosition = Vector3.zero;
            Cursor.lockState = CursorLockMode.Locked;

            foreach (GameObject go in inactiveOnLocalPlayer)
                go.SetActive(false);
        }
        else
        {
            GetComponent<CharacterController>().enabled = false;
            enabled = false;
        }
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        HandleMovement();
        HandleCamera();
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 move = transform.right * horizontal + transform.forward * vertical;

        controller.Move(moveSpeed * Time.deltaTime * move);

        if (Input.GetButtonDown("Jump") && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.LeftShift) && !isSliding)
            StartCoroutine(Slide());
    }

    void HandleCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -cameraRotationLimit, cameraRotationLimit);
        Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        headPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    IEnumerator Slide()
    {
        isSliding = true;
        float originalMoveSpeed = moveSpeed;
        moveSpeed = slideSpeed;

        yield return new WaitForSeconds(slideTime);

        moveSpeed = originalMoveSpeed;
        isSliding = false;
    }
}
