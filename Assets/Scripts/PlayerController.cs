using System.Collections;
using FishNet.Component.Spawning;
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
    public GameObject[] hiddenOnLocalPlayer;
    CharacterController characterController;

    Vector3 velocity;
    float xRotation = 0f;
    bool isClientStarted;
    bool isGrounded;
    bool isSliding;

    public override void OnStartClient()
    {
        base.OnStartClient();

        TrailRenderer trailRenderer = GetComponent<TrailRenderer>();
        trailRenderer.emitting = false;

        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;

            foreach (GameObject go in hiddenOnLocalPlayer)
            {
                go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }

            SetLayerRecursively(gameObject, LayerMask.NameToLayer("LocalPlayer"));
            Destroy(trailRenderer);

            GameManager.Instance.localPlayer = this;
        }
        else
        {
            enabled = false;
        }

        characterController = GetComponent<CharacterController>();
        isClientStarted = true;
    }

    void Update()
    {
        if (!isClientStarted)
            return;

        HandleMovement();
        HandleCamera();
    }

    void HandleMovement()
    {
        isGrounded = characterController.isGrounded;
        
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 move = transform.right * horizontal + transform.forward * vertical;

        characterController.Move(moveSpeed * Time.deltaTime * move);

        if (Input.GetButtonDown("Jump") && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);

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

        Camera.main.transform.rotation = Quaternion.Euler(xRotation, transform.eulerAngles.y, 0f);
        Camera.main.transform.position = cameraPivot.position;

        cameraPivot.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        headPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    IEnumerator Slide()
    {
        isSliding = true;

        float originalMoveSpeed = moveSpeed;
        moveSpeed = slideSpeed;
        SetSlideEffectServerRpc(true);

        yield return new WaitForSeconds(slideTime);

        SetSlideEffectServerRpc(false);
        moveSpeed = originalMoveSpeed;
        isSliding = false;
    }

    [ServerRpc(RequireOwnership = false)]
    void SetSlideEffectServerRpc(bool state)
    {
        SetSlideEffectObserversRpc(state);
    }

    [ObserversRpc(ExcludeOwner = true)]
    void SetSlideEffectObserversRpc(bool state)
    {
        TrailRenderer trailRenderer = GetComponent<TrailRenderer>();
        trailRenderer.emitting = state;
    }

    IEnumerator IDisableSlideEffect(float slideTime, float trailTime)
    {
        yield return new WaitForSeconds(slideTime + trailTime);
        GetComponent<TrailRenderer>().enabled = false;
    }

    void SetLayerRecursively(GameObject go, LayerMask newLayer)
    {
        go.layer = newLayer;

        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, newLayer);
    }
}
