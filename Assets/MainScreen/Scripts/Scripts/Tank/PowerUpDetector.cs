using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Complete
{
    public class PowerUpDetector : MonoBehaviour
    {
        // Tham chiếu đến các GameObject hiệu ứng đặt sẵn, là con của nhân vật.
        [Header("Buff Visual Effect Objects")]
        [Tooltip("Kéo GameObject hiệu ứng Gió (là con của nhân vật) vào đây.")]
        public GameObject windBuffEffectObject;
        [Tooltip("Kéo GameObject hiệu ứng Đất (là con của nhân vật) vào đây.")]
        public GameObject earthBuffEffectObject;
        [Tooltip("Kéo GameObject hiệu ứng Lửa (là con của nhân vật) vào đây.")]
        public GameObject fireBuffEffectObject;

        // Dictionary để theo dõi các coroutine đang hoạt động, đảm bảo không cộng dồn buff.
        private Dictionary<string, Coroutine> m_ActiveCoroutines = new Dictionary<string, Coroutine>();

        // Các tham chiếu đến thành phần cốt lõi của nhân vật.
        private TankShooting m_TankShooting;
        private TankMovement m_TankMovement;
        private TankHealth m_TankHealth;

        // Lưu trữ các giá trị gốc để có thể hoàn tác hiệu ứng.
        private float m_OriginalSpeed;
        private float m_OriginalTurnSpeed;

        // Biến theo dõi hệ số nhân sát thương từ buff Hỏa.
        private float m_CurrentDamageMultiplier = 1f;

        // Awake được gọi khi script được tải.
        private void Awake()
        {
            // Lấy các tham chiếu cần thiết.
            m_TankShooting = GetComponent<TankShooting>();
            m_TankMovement = GetComponent<TankMovement>();
            m_TankHealth = GetComponent<TankHealth>();

            // Kiểm tra an toàn, vô hiệu hóa script nếu thiếu component.
            if (m_TankMovement == null || m_TankShooting == null || m_TankHealth == null)
            {
                Debug.LogError("PowerUpDetector: Thiếu một hoặc nhiều thành phần Tank...");
                enabled = false;
                return;
            }

            // Lưu lại tốc độ gốc.
            if (m_TankMovement != null)
            {
                m_OriginalSpeed = m_TankMovement.m_Speed;
                m_OriginalTurnSpeed = m_TankMovement.m_TurnSpeed;
            }

            // Đảm bảo tất cả các hiệu ứng đều được tắt khi game bắt đầu.
            windBuffEffectObject?.SetActive(false);
            earthBuffEffectObject?.SetActive(false);
            fireBuffEffectObject?.SetActive(false);
        }

        #region Public Methods to Start PowerUps

        // Hàm được gọi bởi quả cầu Hỏa.
        public void PowerUpBaseDamage(float multiplier, float duration)
        {
            HandleTimedPowerUp("BaseDamage", IncreaseBaseDamage(multiplier, duration), fireBuffEffectObject);
        }

        // Hàm được gọi bởi quả cầu Nước.
        public void PowerUpHealing(float healAmount)
        {
            if (m_TankHealth != null)
                m_TankHealth.IncreaseHealth(healAmount);
        }

        // Hàm được gọi bởi quả cầu Gió.
        public void PowerUpSpeed(float speedBoost, float turnSpeedBoost, float duration)
        {
            HandleTimedPowerUp("Speed", IncreaseSpeed(speedBoost, turnSpeedBoost, duration), windBuffEffectObject);
        }

        // Hàm được gọi bởi quả cầu Đất.
        public void PickUpShield(float shieldAmount, float duration)
        {
            if (m_TankHealth != null)
            {
                m_TankHealth.AddShield(shieldAmount, duration);
                HandleTimedPowerUp("ShieldEffect", ShieldEffectDuration(duration), earthBuffEffectObject);
            }
        }

        #endregion

        // --- HÀM QUẢN LÝ BUFF ĐÃ ĐƯỢC NÂNG CẤP VỚI LOGIC "BẬT TRƯỚC, LÀM SAU" ---
        private void HandleTimedPowerUp(string buffName, IEnumerator coroutine, GameObject effectObject)
        {
            // 1. DỌN DẸP BUFF CŨ (NẾU CÓ) - Logic "Reset an toàn"
            if (m_ActiveCoroutines.ContainsKey(buffName))
            {
                // Dừng coroutine cũ để tránh xung đột
                if (m_ActiveCoroutines[buffName] != null)
                    StopCoroutine(m_ActiveCoroutines[buffName]);
                m_ActiveCoroutines.Remove(buffName);

                // Tắt hiệu ứng hình ảnh cũ
                if (effectObject != null)
                    effectObject.SetActive(false);

                // Hoàn tác hiệu ứng cũ (quan trọng!)
                // Ví dụ: Nếu nhặt lại buff Gió, trả tốc độ về bình thường trước khi tăng lại.
                if (buffName == "Speed")
                {
                    m_TankMovement.m_Speed = m_OriginalSpeed;
                    m_TankMovement.m_TurnSpeed = m_OriginalTurnSpeed;
                }
                if (buffName == "BaseDamage")
                {
                    m_CurrentDamageMultiplier = 1f;
                }
            }

            // 2. KÍCH HOẠT BUFF MỚI
            // Bật GameObject hiệu ứng hình ảnh.
            if (effectObject != null)
            {
                effectObject.SetActive(true);
            }

            // Bắt đầu coroutine logic mới và lưu lại tham chiếu.
            m_ActiveCoroutines[buffName] = StartCoroutine(coroutine);
        }

        // Hàm để TankShooting có thể lấy thông tin.
        public float GetCurrentDamageMultiplier()
        {
            return m_CurrentDamageMultiplier;
        }

        #region Coroutines for Timed PowerUps

        // Coroutine cho buff Hỏa.
        private IEnumerator IncreaseBaseDamage(float multiplier, float duration)
        {
            m_CurrentDamageMultiplier = multiplier; // Gán giá trị mới
            yield return new WaitForSeconds(duration); // Chờ hết thời gian
            m_CurrentDamageMultiplier = 1f; // Reset về mặc định

            // Tắt hiệu ứng và dọn dẹp
            if (fireBuffEffectObject != null)
            {
                fireBuffEffectObject.SetActive(false);
            }
            m_ActiveCoroutines.Remove("BaseDamage");
        }

        // Coroutine cho buff Gió.
        private IEnumerator IncreaseSpeed(float speedBoost, float TurnSpeedBoost, float duration)
        {
            m_TankMovement.m_Speed = m_OriginalSpeed + speedBoost;
            m_TankMovement.m_TurnSpeed = m_OriginalTurnSpeed + TurnSpeedBoost;
            yield return new WaitForSeconds(duration);
            m_TankMovement.m_Speed = m_OriginalSpeed;
            m_TankMovement.m_TurnSpeed = m_OriginalTurnSpeed;

            // Tắt hiệu ứng và dọn dẹp
            if (windBuffEffectObject != null)
            {
                windBuffEffectObject.SetActive(false);
            }
            m_ActiveCoroutines.Remove("Speed");
        }

        // Coroutine chỉ để quản lý thời gian và hiệu ứng hình ảnh của khiên.
        private IEnumerator ShieldEffectDuration(float duration)
        {
            // Coroutine này chỉ chờ. Logic của khiên đã được TankHealth xử lý.
            yield return new WaitForSeconds(duration);

            // Tắt hiệu ứng và dọn dẹp
            if (earthBuffEffectObject != null)
            {
                earthBuffEffectObject.SetActive(false);
            }
            m_ActiveCoroutines.Remove("ShieldEffect");
        }

        #endregion
    }
}