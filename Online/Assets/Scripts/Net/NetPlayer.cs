using System.Globalization;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class NetPlayer : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float gravity = -9.81f;

    private Renderer[] rends;

    [Header("Refs")]
    public Transform cameraPivot; 
    public Camera playerCamera;   

    private CharacterController cc;
    private Vector3 velocity;
    private float pitch;

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
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        gm = FindFirstObjectByType<NetGameManager>();
        if (gm != null && IsOwner)
        {
            gm.RegisterPlayerServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        Look();
        Move();
    }

    private void Look()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mx);

        pitch -= my;
        pitch = Mathf.Clamp(pitch, -80f, 80f);

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        cc.Move(move * moveSpeed * Time.deltaTime);

        if (cc.isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }
}
