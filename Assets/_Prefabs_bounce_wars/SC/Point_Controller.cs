using System.Collections;
using UnityEngine;
using Unity.Netcode; // --- THAY ĐỔI NETCODE ---

// --- THAY ĐỔI NETCODE ---
public class Point_Controller : NetworkBehaviour
{
    // --- NETCODE STATE ---
    // Chuyển đổi trạng thái IsCarried sang NetworkVariable.
    // Chỉ Server có quyền thay đổi giá trị này. Mọi người đều có quyền đọc.
    private NetworkVariable<bool> m_IsCarried = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // Lưu trữ ID của người chơi đang mang vật phẩm này. 0 = không ai mang.
    private NetworkVariable<ulong> m_CarrierId = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Thuộc tính public giờ sẽ đọc từ NetworkVariable.
    public bool IsCarried => m_IsCarried.Value;

    // --- CÁC BIẾN VÀ COMMENT GỐC CỦA BẠN ĐƯỢC GIỮ NGUYÊN ---
    [Header("Capture Effect")]
    [SerializeField] private float m_VanishDuration = 0.5f;
    [Header("Following Behaviour")]
    [SerializeField] private float m_DampTime = 0.2f;
    [Tooltip("Tỷ lệ thu nhỏ khi được mang (ví dụ: 0.33 để thu nhỏ còn 1/3).")]
    [SerializeField] private float m_CarriedScaleMultiplier = 0.33f;

    // Các biến lưu trữ
    private Vector3 m_OriginalPosition;
    private Quaternion m_OriginalRotation;
    private Vector3 m_OriginalScale;
    private Collider m_Collider;
    private Renderer m_Renderer;
    private Vector3 m_CurrentVelocity = Vector3.zero;
    private Vector3 m_TargetPosition;

    private void Awake()
    {
        m_OriginalPosition = transform.position;
        m_OriginalRotation = transform.rotation;
        m_OriginalScale = transform.localScale;
        m_Collider = GetComponent<Collider>();
        m_Renderer = GetComponent<Renderer>();
    }

    private void LateUpdate()
    {
        // Logic quay quanh và theo sau không thay đổi, nó sẽ tự động hoạt động
        // vì nó phụ thuộc vào IsCarried, mà IsCarried giờ đã được đồng bộ.
        if (IsCarried && m_Renderer.enabled)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                m_TargetPosition,
                ref m_CurrentVelocity,
                m_DampTime
            );
            transform.Rotate(Vector3.up, 45f * Time.deltaTime, Space.World);
        }
    }

    // --- THAY ĐỔI NETCODE ---
    // Hàm này được gọi bởi Server từ TankFlagCarrier.
    public void CaptureAndFollow(ulong carrierId)
    {
        // Đảm bảo logic thay đổi trạng thái chỉ chạy trên Server.
        if (!IsServer) return;

        // Server cập nhật trạng thái trên mạng.
        m_IsCarried.Value = true;
        m_CarrierId.Value = carrierId;

        // Server ra lệnh cho tất cả Client thực hiện hiệu ứng hình ảnh.
        CaptureVisualsClientRpc();
    }

    // --- THAY ĐỔI NETCODE ---
    // Hàm này được Server gọi, và thực thi trên TẤT CẢ các Client.
    [ClientRpc]
    private void CaptureVisualsClientRpc()
    {
        // Logic hiệu ứng hình ảnh từ hàm CaptureRoutine cũ được chuyển vào đây.
        StopAllCoroutines(); // Dừng các coroutine cũ nếu có
        StartCoroutine(ScaleOverTime(m_OriginalScale * m_CarriedScaleMultiplier, m_VanishDuration));
        if (m_Collider != null) m_Collider.enabled = false;

        // Chúng ta không cần đợi ở đây nữa, TankFlagCarrier sẽ xử lý việc hiển thị lại.
        // Hiệu ứng quay quanh sẽ bắt đầu ngay khi vật phẩm được thêm vào NetworkList của Carrier.
        // Chỉ cần đảm bảo Renderer được bật để logic quay trong LateUpdate hoạt động.
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

    // Hàm này không thay đổi, nó được gọi mỗi frame từ TankFlagCarrier trên tất cả client.
    public void SetTargetPosition(Vector3 newPosition)
    {
        m_TargetPosition = newPosition;
    }

    // --- THAY ĐỔI NETCODE ---
    // Hàm này được gọi bởi Server từ TankFlagCarrier khi ghi điểm hoặc chết.
    public void ResetState()
    {
        // Đảm bảo logic thay đổi trạng thái chỉ chạy trên Server.
        if (!IsServer) return;

        // Server cập nhật trạng thái trên mạng.
        m_IsCarried.Value = false;
        m_CarrierId.Value = 0; // 0 để biểu thị không có ai mang.

        // Server ra lệnh cho tất cả Client thực hiện hiệu ứng reset.
        ResetVisualsClientRpc();
    }

    // --- THAY ĐỔI NETCODE ---
    // Hàm này được Server gọi, và thực thi trên TẤT CẢ các Client.
    [ClientRpc]
    private void ResetVisualsClientRpc()
    {
        // Logic reset từ hàm ResetState cũ được chuyển vào đây.
        StopAllCoroutines();
        transform.SetParent(null); // Đảm bảo nó không còn là con của bất kỳ xe tăng nào.
        transform.position = m_OriginalPosition;
        transform.rotation = m_OriginalRotation;
        transform.localScale = m_OriginalScale;

        if (m_Collider != null) m_Collider.enabled = true;
        if (m_Renderer != null) m_Renderer.enabled = true;
    }
}