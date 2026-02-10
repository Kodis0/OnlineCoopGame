using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class NetPlayer : NetworkBehaviour
{
    [Header("Speed")]
    [SerializeField] private float walkSpeed = 7.5f;
    [SerializeField] private float runSpeed = 11.5f;

    [Header("Air control (0..1)")]
    [Tooltip("0 = почти нет управления в воздухе, 1 = как на земле")]
    [Range(0f, 1f)]
    [SerializeField] private float airControl = 0.25f;

    [Header("Jump / Gravity (snappy)")]
    [SerializeField] private float jumpHeight = 1.15f;
    [SerializeField] private float gravity = -38f;

    [Tooltip("Ускоряет падение (чтобы не висеть в воздухе)")]
    [SerializeField] private float fallMultiplier = 1.6f;

    [Tooltip("Сильнее прижимает к земле, чтобы не подпрыгивал на склонах")]
    [SerializeField] private float groundStick = -6f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2.0f;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Camera playerCamera;

    [Header("Run key")]
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;

    [Header("Scale Compensation (player scale = 2)")]
    [SerializeField] private bool compensateSpeedForScale = true;

    private CharacterController cc;
    private float pitch;

    private Vector3 planarVel;
    private float verticalVel;

    private Renderer[] rends;
    private NetGameManager gm;

    public override void OnNetworkSpawn()
    {
        cc = GetComponent<CharacterController>();

        if (playerCamera != null)
            playerCamera.enabled = IsOwner;

        rends = GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
            r.enabled = !IsOwner;

        if (cameraPivot != null && IsOwner)
        {
            //Cursor.lockState = CursorLockMode.Locked;
            //Cursor.visible = false;
        }

        gm = FindFirstObjectByType<NetGameManager>();
        if (gm != null && IsOwner)
            gm.RegisterPlayerServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    private void Update()
    {
        if (!IsOwner) return;

        bool inGame = SceneManager.GetActiveScene().name == "Game";

        if (inGame)
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Look();
            Move();
        }
        else
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }


    private void Look()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mx);

        pitch -= my;
        pitch = Mathf.Clamp(pitch, -85f, 85f);

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        bool grounded = cc.isGrounded;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(x, 0f, z);
        if (input.sqrMagnitude > 1f) input.Normalize();

        bool running = Input.GetKey(runKey);
        float speed = running ? runSpeed : walkSpeed;

        if (compensateSpeedForScale)
        {
            float s = Mathf.Max(0.0001f, transform.lossyScale.x);
            speed /= s;
        }

        Vector3 wishDir = transform.right * input.x + transform.forward * input.z;
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

        Vector3 desiredPlanar = wishDir * speed;

        if (grounded)
        {
            planarVel = desiredPlanar;

            if (verticalVel < 0f) verticalVel = groundStick;

            if (Input.GetButtonDown("Jump"))
                verticalVel = Mathf.Sqrt(2f * jumpHeight * -gravity);
        }
        else
        {
            planarVel = Vector3.Lerp(planarVel, desiredPlanar, airControl);

            float g = gravity;
            if (verticalVel < 0f) g *= fallMultiplier;

            verticalVel += g * Time.deltaTime;

            ApplyMotion();
            return;
        }

        verticalVel += gravity * Time.deltaTime;

        ApplyMotion();
    }

    private void ApplyMotion()
    {
        Vector3 motion = (planarVel + Vector3.up * verticalVel) * Time.deltaTime;
        cc.Move(motion);

        if ((cc.collisionFlags & CollisionFlags.Above) != 0 && verticalVel > 0f)
            verticalVel = 0f;

        if (cc.isGrounded && verticalVel < 0f)
            verticalVel = groundStick;
    }
}
