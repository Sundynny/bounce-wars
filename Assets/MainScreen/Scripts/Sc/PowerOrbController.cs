using System.Collections;
using UnityEngine;

// Đặt tên namespace để nhất quán với dự án của bạn
namespace Tanks.Complete
{
    public class PowerOrbController : MonoBehaviour
    {
        // --- PHẦN LẤY TỪ ElementalOrb.cs ---
        // Enum để xác định loại nguyên tố của quả cầu
        public enum ElementType
        {
            None,
            Fire,
            Water,
            Wind,
            Earth
        }

        [Header("Orb Settings")]
        [Tooltip("Loại nguyên tố của quả cầu này.")]
        public ElementType elementType;


        // --- PHẦN LẤY TỪ Point_Controller.cs ---
        // Biến để theo dõi trạng thái, chỉ đọc từ bên ngoài
        private bool m_IsCarried = false;
        public bool IsCarried => m_IsCarried;

        [Header("Capture Effect")]
        [Tooltip("Thời gian để quả cầu thu nhỏ và bay về phía người chơi.")]
        [SerializeField] private float m_CaptureDuration = 0.5f;

        [Header("Following Behaviour")]
        [Tooltip("Độ trễ khi quả cầu bay theo sau, càng nhỏ càng bám sát.")]
        [SerializeField] private float m_DampTime = 0.2f;
        [Tooltip("Tỷ lệ thu nhỏ khi được mang (ví dụ: 0.5 để thu nhỏ còn một nửa).")]
        [SerializeField] private float m_CarriedScaleMultiplier = 0.5f;

        // Các biến lưu trữ trạng thái gốc và các component cần thiết
        private Vector3 m_OriginalPosition;
        private Quaternion m_OriginalRotation;
        private Vector3 m_OriginalScale;
        private Collider m_Collider;
        private Renderer m_Renderer;

        // Các biến cho việc di chuyển mượt mà
        private Vector3 m_CurrentVelocity = Vector3.zero;
        private Vector3 m_TargetPosition;

        // Biến để ngăn việc nhặt lại ngay sau khi được thả ra
        private bool m_IsResettable = true;


        private void Awake()
        {
            // Lưu lại trạng thái ban đầu của quả cầu
            m_OriginalPosition = transform.position;
            m_OriginalRotation = transform.rotation;
            m_OriginalScale = transform.localScale;

            // Lấy các component để sử dụng sau này
            m_Collider = GetComponent<Collider>();
            m_Renderer = GetComponent<Renderer>();
        }

        // LateUpdate được dùng để xử lý di chuyển, đảm bảo nhân vật đã di chuyển xong trong frame đó
        private void LateUpdate()
        {
            // Nếu đang được mang và đang hiện hình, thì di chuyển mượt mà tới vị trí mục tiêu
            if (m_IsCarried && m_Renderer.enabled)
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    m_TargetPosition,
                    ref m_CurrentVelocity,
                    m_DampTime
                );
                // Vẫn giữ hiệu ứng xoay tự thân cho đẹp mắt
                transform.Rotate(Vector3.up, 45f * Time.deltaTime, Space.World);
            }
        }

        // Hàm được gọi bởi OrbCollector khi người chơi nhặt quả cầu
        public void Capture(Transform parent)
        {
            // Nếu đã được mang hoặc chưa sẵn sàng để nhặt, thì bỏ qua
            if (m_IsCarried || !m_IsResettable) return;

            m_IsCarried = true;
            m_IsResettable = false; // Đánh dấu là không thể reset ngay
            transform.SetParent(parent); // Gán người chơi làm "cha" để dễ quản lý
            StartCoroutine(CaptureRoutine());
        }

        // Coroutine xử lý hiệu ứng khi được nhặt
        private IEnumerator CaptureRoutine()
        {
            // Vô hiệu hóa va chạm để không nhặt lại chính nó
            if (m_Collider != null) m_Collider.enabled = false;

            // Bắt đầu quá trình thu nhỏ
            StartCoroutine(ScaleOverTime(m_OriginalScale * m_CarriedScaleMultiplier, m_CaptureDuration));

            // Chờ một chút trước khi hiện hình lại
            yield return new WaitForSeconds(m_CaptureDuration);

            // Ẩn quả cầu đi một lát (tùy chọn, có thể bỏ qua nếu muốn thấy nó bay về)
            // if (m_Renderer != null) m_Renderer.enabled = false;

            // Vị trí xuất hiện đầu tiên sẽ là ở gần người chơi
            if (transform.parent != null)
            {
                transform.position = transform.parent.position + Vector3.up * 2f;
            }

            // Hiện hình lại quả cầu
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

        // Hàm được gọi mỗi frame bởi OrbCollector để cập nhật vị trí bay theo
        public void SetTargetPosition(Vector3 newPosition)
        {
            m_TargetPosition = newPosition;
        }

        // Hàm được gọi bởi OrbCollector khi quả cầu được "tiêu thụ"
        public void Consume()
        {
            // Dừng mọi coroutine, ẩn hình và chuẩn bị để bị hủy
            StopAllCoroutines();
            if (m_Renderer != null) m_Renderer.enabled = false;
            if (m_Collider != null) m_Collider.enabled = false;

            // Tự hủy sau một khoảng trễ nhỏ
            Destroy(gameObject, 1f);
        }

        // Hàm này có thể dùng nếu bạn muốn quả cầu hồi sinh tại chỗ thay vì bị hủy
        public void ResetState()
        {
            StopAllCoroutines();
            m_IsCarried = false;
            transform.SetParent(null); // Tách khỏi người chơi
            transform.position = m_OriginalPosition;
            transform.rotation = m_OriginalRotation;
            transform.localScale = m_OriginalScale;

            if (m_Renderer != null) m_Renderer.enabled = true;
            if (m_Collider != null) m_Collider.enabled = true;

            // Bắt đầu coroutine để quả cầu không thể bị nhặt lại ngay lập tức
            StartCoroutine(ResetCooldown());
        }

        private IEnumerator ResetCooldown()
        {
            m_IsResettable = false;
            yield return new WaitForSeconds(1.5f); // Chờ 1.5 giây
            m_IsResettable = true;
        }
    }
}