using System.Collections;
using UnityEngine;

public class Point_Controller : MonoBehaviour
{
    private bool m_IsCarried = false;
    public bool IsCarried => m_IsCarried;

    [Header("Capture Effect")]
    [SerializeField] private float m_VanishDuration = 0.5f;

    [Header("Following Behaviour")]
    [SerializeField] private float m_DampTime = 0.2f;
    [Tooltip("Tỷ lệ thu nhỏ khi được mang (ví dụ: 0.33 để thu nhỏ còn 1/3).")]
    [SerializeField] private float m_CarriedScaleMultiplier = 0.33f;

    // Các biến lưu trữ
    private Vector3 m_OriginalPosition;
    private Quaternion m_OriginalRotation;
    private Vector3 m_OriginalScale; // <-- THÊM MỚI: Lưu kích thước gốc
    private Collider m_Collider;
    private Renderer m_Renderer;
    private Vector3 m_CurrentVelocity = Vector3.zero;
    private Vector3 m_TargetPosition;

    private void Awake()
    {
        m_OriginalPosition = transform.position;
        m_OriginalRotation = transform.rotation;
        m_OriginalScale = transform.localScale; // <-- THÊM MỚI: Lưu kích thước gốc
        m_Collider = GetComponent<Collider>();
        m_Renderer = GetComponent<Renderer>();
    }

    private void LateUpdate()
    {
        if (m_IsCarried && m_Renderer.enabled)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                m_TargetPosition,
                ref m_CurrentVelocity,
                m_DampTime
            );
            // Vẫn giữ xoay tự thân
            transform.Rotate(Vector3.up, 45f * Time.deltaTime, Space.World);
        }
    }

    public void Capture()
    {
        if (m_IsCarried) return;
        m_IsCarried = true;
        StartCoroutine(CaptureRoutine());
    }

    private IEnumerator CaptureRoutine()
    {
        // Thu nhỏ dần quả cầu
        StartCoroutine(ScaleOverTime(m_OriginalScale * m_CarriedScaleMultiplier, m_VanishDuration));

        if (m_Collider != null) m_Collider.enabled = false;

        // Chờ biến mất
        yield return new WaitForSeconds(m_VanishDuration);

        if (m_Renderer != null) m_Renderer.enabled = false;

        // Vị trí xuất hiện đầu tiên sẽ là ở sau xe tăng
        if (transform.parent != null)
        {
            transform.position = transform.parent.position - transform.parent.forward * 2f;
        }

        if (m_Renderer != null) m_Renderer.enabled = true;
    }

    // Coroutine để thay đổi kích thước mượt mà
    private IEnumerator ScaleOverTime(Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        float time = 0;
        while (time < duration)
        {
            transform.localScale = Vector3.Lerp(startScale, targetScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        transform.localScale = targetScale;
    }

    public void SetTargetPosition(Vector3 newPosition)
    {
        m_TargetPosition = newPosition;
    }

    public void ResetState()
    {
        StopAllCoroutines();
        m_IsCarried = false;
        transform.position = m_OriginalPosition;
        transform.rotation = m_OriginalRotation;
        transform.localScale = m_OriginalScale; // <-- THÊM MỚI: Reset kích thước

        if (m_Collider != null) m_Collider.enabled = true;
        if (m_Renderer != null) m_Renderer.enabled = true;
    }
}