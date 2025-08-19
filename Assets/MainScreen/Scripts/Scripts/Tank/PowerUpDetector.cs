using System.Collections;
using System.Collections.Generic; // Cần thiết để sử dụng Dictionary
using UnityEngine;

namespace Tanks.Complete
{
    public class PowerUpDetector : MonoBehaviour
    {
        // Sử dụng Dictionary để theo dõi các coroutine đang hoạt động cho từng loại PowerUp.
        // Điều này cho phép nhiều hiệu ứng khác nhau hoạt động cùng lúc.
        private Dictionary<PowerUp.PowerUpType, Coroutine> m_ActiveCoroutines = new Dictionary<PowerUp.PowerUpType, Coroutine>();

        // Các tham chiếu đến thành phần của xe tăng
        private TankShooting m_TankShooting;
        private TankMovement m_TankMovement;
        private TankHealth m_TankHealth;
        private PowerUpHUD m_PowerUpHUD;

        // Lưu trữ các giá trị gốc để áp dụng/hoàn tác hiệu ứng một cách chính xác
        private float m_OriginalSpeed;
        private float m_OriginalTurnSpeed;
        private float m_OriginalShotCooldown;

        private void Awake()
        {
            // Lấy tham chiếu đến các thành phần
            m_TankShooting = GetComponent<TankShooting>();
            m_TankMovement = GetComponent<TankMovement>();
            m_TankHealth = GetComponent<TankHealth>();
            m_PowerUpHUD = GetComponentInChildren<PowerUpHUD>();

            // Kiểm tra các thành phần quan trọng
            if (m_TankMovement == null || m_TankShooting == null || m_TankHealth == null)
            {
                Debug.LogError("PowerUpDetector: Thiếu một hoặc nhiều thành phần Tank (Movement, Shooting, Health) trên " + gameObject.name + ". Script sẽ bị vô hiệu hóa.");
                enabled = false;
                return;
            }

            // Lưu trữ các giá trị gốc khi game bắt đầu
            m_OriginalSpeed = m_TankMovement.m_Speed;
            m_OriginalTurnSpeed = m_TankMovement.m_TurnSpeed;
            m_OriginalShotCooldown = m_TankShooting.m_ShotCooldown;
        }

        #region Public Methods to Start PowerUps

        // Áp dụng tăng tốc tạm thời cho xe tăng
        public void PowerUpSpeed(float speedBoost, float turnSpeedBoost, float duration)
        {
            HandleTimedPowerUp(PowerUp.PowerUpType.Speed, IncreaseSpeed(speedBoost, turnSpeedBoost, duration));
        }

        // Áp dụng tăng tốc độ bắn tạm thời cho xe tăng
        public void PowerUpShoootingRate(float cooldownReduction, float duration)
        {
            HandleTimedPowerUp(PowerUp.PowerUpType.ShootingBonus, IncreaseShootingRate(cooldownReduction, duration));
        }

        // Cung cấp cho xe tăng một tấm khiên tạm thời
        public void PickUpShield(float shieldAmount, float duration)
        {
            HandleTimedPowerUp(PowerUp.PowerUpType.DamageReduction, ActivateShield(shieldAmount, duration));
        }

        // Làm cho xe tăng bất tử trong một khoảng thời gian
        public void PowerUpInvincibility(float duration)
        {
            HandleTimedPowerUp(PowerUp.PowerUpType.Invincibility, ActivateInvincibility(duration));
        }

        // Các hiệu ứng tức thời không cần quản lý thời gian
        public void PowerUpHealing(float healAmount)
        {
            m_TankHealth.IncreaseHealth(healAmount);
            if (m_PowerUpHUD != null)
                m_PowerUpHUD.SetActivePowerUp(PowerUp.PowerUpType.Healing, 1.0f);
        }

        public void PowerUpSpecialShell(float damageMultiplier)
        {
            // Hiệu ứng này không có thời gian, nó chỉ áp dụng cho lần bắn tiếp theo.
            // Nếu bạn muốn nó cũng có thể làm mới, bạn sẽ cần một logic phức tạp hơn.
            // Hiện tại, nó chỉ đơn giản là trang bị.
            if (m_PowerUpHUD != null)
                m_PowerUpHUD.SetActivePowerUp(PowerUp.PowerUpType.DamageMultiplier, 0f);

            m_TankShooting.EquipSpecialShell(damageMultiplier);
        }

        #endregion

        // Hàm quản lý trung tâm cho các hiệu ứng có thời gian
        private void HandleTimedPowerUp(PowerUp.PowerUpType type, IEnumerator coroutine)
        {
            // Nếu hiệu ứng này đã được kích hoạt, hãy dừng coroutine cũ để làm mới thời gian
            if (m_ActiveCoroutines.ContainsKey(type))
            {
                StopCoroutine(m_ActiveCoroutines[type]);
            }

            // Bắt đầu coroutine mới và lưu nó vào Dictionary
            m_ActiveCoroutines[type] = StartCoroutine(coroutine);
        }


        #region Coroutines for Timed PowerUps

        private IEnumerator IncreaseSpeed(float speedBoost, float TurnSpeedBoost, float duration)
        {
            if (m_PowerUpHUD != null)
                m_PowerUpHUD.SetActivePowerUp(PowerUp.PowerUpType.Speed, duration);

            // Áp dụng hiệu ứng dựa trên giá trị gốc
            m_TankMovement.m_Speed = m_OriginalSpeed + speedBoost;
            m_TankMovement.m_TurnSpeed = m_OriginalTurnSpeed + TurnSpeedBoost;

            yield return new WaitForSeconds(duration);

            // Hoàn tác hiệu ứng, quay về giá trị gốc
            m_TankMovement.m_Speed = m_OriginalSpeed;
            m_TankMovement.m_TurnSpeed = m_OriginalTurnSpeed;

            // Xóa coroutine khỏi danh sách đang hoạt động
            m_ActiveCoroutines.Remove(PowerUp.PowerUpType.Speed);
        }

        private IEnumerator IncreaseShootingRate(float cooldownReduction, float duration)
        {
            if (cooldownReduction > 0)
            {
                if (m_PowerUpHUD != null)
                    m_PowerUpHUD.SetActivePowerUp(PowerUp.PowerUpType.ShootingBonus, duration);

                // Áp dụng hiệu ứng
                m_TankShooting.m_ShotCooldown = m_OriginalShotCooldown * cooldownReduction;

                yield return new WaitForSeconds(duration);

                // Hoàn tác hiệu ứng
                m_TankShooting.m_ShotCooldown = m_OriginalShotCooldown;
                m_ActiveCoroutines.Remove(PowerUp.PowerUpType.ShootingBonus);
            }
        }

        private IEnumerator ActivateShield(float shieldAmount, float duration)
        {
            if (m_PowerUpHUD != null)
                m_PowerUpHUD.SetActivePowerUp(PowerUp.PowerUpType.DamageReduction, duration);

            // Bật khiên (nếu nó chưa bật)
            if (!m_TankHealth.m_HasShield)
                m_TankHealth.ToggleShield(shieldAmount);

            yield return new WaitForSeconds(duration);

            // Tắt khiên (nếu nó vẫn đang bật)
            if (m_TankHealth.m_HasShield)
                m_TankHealth.ToggleShield(shieldAmount);

            m_ActiveCoroutines.Remove(PowerUp.PowerUpType.DamageReduction);
        }

        private IEnumerator ActivateInvincibility(float duration)
        {
            if (m_PowerUpHUD != null)
                m_PowerUpHUD.SetActivePowerUp(PowerUp.PowerUpType.Invincibility, duration);

            m_TankHealth.ToggleInvincibility();

            yield return new WaitForSeconds(duration);

            m_TankHealth.ToggleInvincibility();
            m_ActiveCoroutines.Remove(PowerUp.PowerUpType.Invincibility);
        }

        #endregion
    }
}