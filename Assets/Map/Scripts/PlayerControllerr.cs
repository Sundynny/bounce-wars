using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControllerr : MonoBehaviour
{
    [Header("Input Names")]
    public string horizontalInput = "Horizontal_P1";
    public string verticalInput = "Vertical_P1";
    public KeyCode fireKey = KeyCode.Space;
    public KeyCode jumpKey = KeyCode.LeftShift; // nút nhảy (có thể đổi)

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 10f;

    private Rigidbody rb;
    private bool isGrounded = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Ngăn nhân vật bị ngã khi va chạm
    }

    void Update()
    {
        // Nhận input di chuyển
        float moveX = Input.GetAxis(horizontalInput);
        float moveZ = Input.GetAxis(verticalInput);

        Vector3 move = new Vector3(moveX, 0, moveZ).normalized;

        // Nếu có di chuyển → xoay nhân vật
        if (move.magnitude > 0.1f)
        {
            transform.forward = move;
        }

        // Di chuyển bằng Rigidbody
        Vector3 moveWorld = move * moveSpeed * Time.deltaTime;
        rb.MovePosition(rb.position + transform.TransformDirection(moveWorld));

        // Nhảy
        if (isGrounded && Input.GetKeyDown(jumpKey))
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }

        // Bắn
        if (Input.GetKeyDown(fireKey))
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            Rigidbody brb = bullet.GetComponent<Rigidbody>();
            brb.linearVelocity = firePoint.forward * bulletSpeed;
            Destroy(bullet, 3f);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Kiểm tra chạm đất để nhảy lại
        if (collision.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
        }
    }
}
