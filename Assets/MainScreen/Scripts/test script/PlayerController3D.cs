using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController3D : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float rotationSpeed = 300f;
    public float jumpHeight = 4f;
    public float gravity = -9.81f;
    public float groundedStick = -2f;

    private CharacterController controller;
    private Vector3 velocity;
    private Transform cam;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main != null ? Camera.main.transform : null;
    }

    void Update()
    {
        // Kiểm tra chạm đất
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = groundedStick;

        // Input mũi tên
        float h = (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) + (Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f);
        float v = (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) + (Input.GetKey(KeyCode.DownArrow) ? -1f : 0f);

        // Hướng di chuyển theo camera
        Vector3 moveDir;
        if (cam != null)
        {
            Vector3 camF = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 camR = Vector3.Scale(cam.right, new Vector3(1, 0, 1)).normalized;
            moveDir = (camF * v + camR * h).normalized;
        }
        else
        {
            moveDir = new Vector3(h, 0f, v).normalized;
        }

        // Di chuyển
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            controller.Move(moveDir * moveSpeed * Time.deltaTime);
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // Nhảy
        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Áp dụng trọng lực
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
