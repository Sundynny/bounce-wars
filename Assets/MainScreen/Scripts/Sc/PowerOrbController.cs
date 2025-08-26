using UnityEngine;
using System.Collections;

namespace Tanks.Complete
{
    public class PowerOrbController : MonoBehaviour
    {
        // Enum để xác định loại nguyên tố của quả cầu.
        public enum ElementType
        {
            None,
            Fire,
            Water,
            Wind,
            Earth
        }

        [Header("Orb Type")]
        [Tooltip("Loại nguyên tố của quả cầu này. Hãy thiết lập đúng cho từng Prefab.")]
        public ElementType elementType;

        [Header("Instant Buff Settings")]
        [Tooltip("Thời gian tồn tại của buff (giây). Chỉ áp dụng cho các buff có thời gian.")]
        public float effectDuration = 10f;
        [Tooltip("Sức mạnh của hiệu ứng (ví dụ: lượng máu hồi, lượng giáp, tốc độ tăng thêm).")]
        public float effectStrength = 15f;
        [Tooltip("Hệ số nhân sát thương cho buff Lửa.")]
        public float damageMultiplier = 1.5f;

        // Trạng thái của quả cầu
        private bool m_IsCarried = false; // Đã được thu thập chưa?
        public bool IsCarried => m_IsCarried;
        private bool m_HasGivenBuff = false; // Đã đưa buff tức thì chưa?
        private bool m_IsCollectable = true; // Có thể được nhặt không?

        [Header("Visual Effects on Capture")]
        [SerializeField] private float m_DampTime = 0.2f;
        [SerializeField] private float m_CarriedScaleMultiplier = 0.5f;
        [SerializeField] private float m_CaptureAnimationTime = 0.5f;

        // Các biến nội bộ
        private Collider m_Collider;
        private Renderer m_Renderer;
        private Vector3 m_CurrentVelocity = Vector3.zero;
        private Vector3 m_TargetPosition;

        private void Awake()
        {
            m_Collider = GetComponent<Collider>();
            m_Renderer = GetComponent<Renderer>();
        }

        // --- HÀM CÔNG KHAI ĐỂ ORBCOLLECTOR ĐIỀU KHIỂN ---

        /// <summary>
        /// Áp dụng hiệu ứng buff tức thì. Được gọi TỪ BÊN NGOÀI bởi OrbCollector.
        /// </summary>
        public void ApplyInstantBuff(PowerUpDetector detector)
        {
            // Nếu chưa đưa buff và có detector hợp lệ...
            if (!m_HasGivenBuff && detector != null)
            {
                // Đánh dấu là đã đưa buff để không lặp lại
                m_HasGivenBuff = true;

                // Áp dụng hiệu ứng
                switch (elementType)
                {
                    case ElementType.Fire:
                        detector.PowerUpBaseDamage(damageMultiplier, effectDuration);
                        break;
                    case ElementType.Water:
                        detector.PowerUpHealing(effectStrength);
                        break;
                    case ElementType.Wind:
                        detector.PowerUpSpeed(effectStrength, effectStrength, effectDuration);
                        break;
                    case ElementType.Earth:
                        detector.PickUpShield(effectStrength, effectDuration);
                        break;
                }
            }
        }

        /// <summary>
        /// Bắt đầu quá trình được thu thập và bay theo sau. Được gọi TỪ BÊN NGOÀI bởi OrbCollector.
        /// </summary>
        public void Capture(Transform parent)
        {
            if (m_IsCarried || !m_IsCollectable) return;

            m_IsCarried = true;
            m_IsCollectable = false;
            transform.SetParent(parent);

            // Tắt va chạm để không kích hoạt OnTriggerEnter nữa
            if (m_Collider != null) m_Collider.enabled = false;

            StartCoroutine(CaptureRoutine());
        }

        // --- CÁC HÀM PRIVATE VÀ COROUTINE ---

        // LateUpdate xử lý việc bay theo sau
        private void LateUpdate()
        {
            if (m_IsCarried && m_Renderer != null && m_Renderer.enabled)
            {
                transform.position = Vector3.SmoothDamp(transform.position, m_TargetPosition, ref m_CurrentVelocity, m_DampTime);
                transform.Rotate(Vector3.up, 45f * Time.deltaTime, Space.World);
            }
        }

        // Coroutine xử lý hiệu ứng hình ảnh khi được nhặt
        private IEnumerator CaptureRoutine()
        {
            Vector3 originalScale = transform.localScale;
            StartCoroutine(ScaleOverTime(originalScale * m_CarriedScaleMultiplier, m_CaptureAnimationTime));
            yield return new WaitForSeconds(m_CaptureAnimationTime);

            if (transform.parent != null)
            {
                transform.position = transform.parent.position + Vector3.up * 2f;
            }
            if (m_Renderer != null) m_Renderer.enabled = true;
        }

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

        // Hàm được gọi khi quả cầu bị tiêu thụ bởi kỹ năng
        public void Consume()
        {
            // Tự phá hủy
            Destroy(gameObject);
        }
    }
}