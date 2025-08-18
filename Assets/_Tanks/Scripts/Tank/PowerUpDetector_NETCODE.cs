using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode; // --- THAY ĐỔI NETCODE ---

namespace Tanks.Complete
{
    // --- THAY ĐỔI NETCODE ---
    // Script này cần kế thừa từ NetworkBehaviour để có thể sử dụng ServerRpc và ClientRpc.
    public class PowerUpDetector_NETCODE : NetworkBehaviour
    {
        // --- CÁC BIẾN VÀ COMMENT GỐC CỦA BẠN ĐƯỢC GIỮ NGUYÊN ---

        // Sử dụng Dictionary để theo dõi các coroutine đang hoạt động cho từng loại PowerUp.
        // Điều này cho phép nhiều hiệu ứng khác nhau hoạt động cùng lúc.
        // --- THAY ĐỔI NETCODE ---
        // Dictionary này giờ sẽ chỉ được sử dụng trên Server để theo dõi thời gian hiệu lực của power-up.
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
        // --- THAY ĐỔI NETCODE ---
        // Các hàm public này giờ đây sẽ chỉ gọi một ServerRpc để yêu cầu Server áp dụng hiệu ứng.
        // Chúng sẽ được gọi từ script của vật phẩm (ví dụ: ElementalOrb).

        // Áp dụng tăng tốc tạm thời cho xe tăng
        public void PowerUpSpeed(float speedBoost, float turnSpeedBoost, float duration)
        {
            ApplySpeedPowerUpServerRpc(speedBoost, turnSpeedBoost, duration);
        }

        // Áp dụng tăng tốc độ bắn tạm thời cho xe tăng
        public void PowerUpShoootingRate(float cooldownReduction, float duration)
        {
            ApplyShootingRatePowerUpServerRpc(cooldownReduction, duration);
        }

        // Cung cấp cho xe tăng một tấm khiên tạm thời
        public void PickUpShield(float shieldAmount, float duration)
        {
            ApplyShieldPowerUpServerRpc(shieldAmount, duration);
        }

        // Làm cho xe tăng bất tử trong một khoảng thời gian
        public void PowerUpInvincibility(float duration)
        {
            ApplyInvincibilityPowerUpServerRpc(duration);
        }

        // Các hiệu ứng tức thời không cần quản lý thời gian
        public void PowerUpHealing(float healAmount)
        {
            // Đối với hồi máu, chúng ta đã có sẵn hàm xử lý trên Server trong TankHealth
            m_TankHealth.IncreaseHealth(healAmount);
            // Hiệu ứng HUD sẽ được kích hoạt riêng bằng ClientRpc
            ShowHealingEffectClientRpc();
        }

        public void PowerUpSpecialShell(float damageMultiplier)
        {
            // Trang bị đạn đặc biệt cũng cần được xử lý trên Server
            ApplySpecialShellServerRpc(damageMultiplier);
        }

        #endregion

        #region Server-Side Logic (ServerRpcs and Coroutines)

        // --- THAY ĐỔI NETCODE ---
        // [ServerRpc] - Hàm này được Client gọi, nhưng chỉ thực thi trên Server.
        [ServerRpc]
        private void ApplySpeedPowerUpServerRpc(float speedBoost, float turnSpeedBoost, float duration)
        {
            // Server sẽ bắt đầu Coroutine để quản lý hiệu ứng và thời gian.
            HandleTimedPowerUpOnServer(PowerUp.PowerUpType.Speed, IncreaseSpeed(speedBoost, turnSpeedBoost, duration));
            // Sau khi áp dụng, Server ra lệnh cho tất cả Client hiển thị hiệu ứng hình ảnh.
            ShowSpeedEffectClientRpc(duration);
        }

        [ServerRpc]
        private void ApplyShootingRatePowerUpServerRpc(float cooldownReduction, float duration)
        {
            HandleTimedPowerUpOnServer(PowerUp.PowerUpType.ShootingBonus, IncreaseShootingRate(cooldownReduction, duration));
            ShowShootingRateEffectClientRpc(duration);
        }

        [ServerRpc]
        private void ApplyShieldPowerUpServerRpc(float shieldAmount, float duration)
        {
            HandleTimedPowerUpOnServer(PowerUp.PowerUpType.DamageReduction, ActivateShield(shieldAmount, duration));
            ShowShieldEffectClientRpc(duration);
        }

        [ServerRpc]
        private void ApplyInvincibilityPowerUpServerRpc(float duration)
        {
            HandleTimedPowerUpOnServer(PowerUp.PowerUpType.Invincibility, ActivateInvincibility(duration));
            ShowInvincibilityEffectClientRpc(duration);
        }

        [ServerRpc]
        private void ApplySpecialShellServerRpc(float damageMultiplier)
        {
            // Hiệu ứng tức thời trên server
            m_TankShooting.EquipSpecialShell(damageMultiplier);
            // Ra lệnh cho client hiển thị HUD
            ShowSpecialShellEffectClientRpc();
        }

        // Hàm quản lý trung tâm cho các hiệu ứng có thời gian, chỉ chạy trên Server
        private void HandleTimedPowerUpOnServer(PowerUp.PowerUpType type, IEnumerator coroutine)
        {
            if (!IsServer) return; // Đảm bảo an toàn

            // Nếu hiệu ứng này đã được kích hoạt, hãy dừng coroutine cũ để làm mới thời gian
            if (m_ActiveCoroutines.ContainsKey(type))
            {
                StopCoroutine(m_ActiveCoroutines[type]);
            }
            // Bắt đầu coroutine mới và lưu nó vào Dictionary
            m_ActiveCoroutines[type] = StartCoroutine(coroutine);
        }

        #region Coroutines for Timed PowerUps (RUN ON SERVER ONLY)
        // Các Coroutine này giờ đây sẽ chỉ chạy trên Server để thay đổi chỉ số thực của xe tăng.

        private IEnumerator IncreaseSpeed(float speedBoost, float TurnSpeedBoost, float duration)
        {
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
            m_TankHealth.ToggleInvincibility();
            yield return new WaitForSeconds(duration);
            m_TankHealth.ToggleInvincibility();
            m_ActiveCoroutines.Remove(PowerUp.PowerUpType.Invincibility);
        }

        #endregion
        #endregion

        #region Client-Side Effects (ClientRpcs)

        // --- THAY ĐỔI NETCODE ---
        // Các hàm [ClientRpc] này được Server gọi để ra lệnh cho TẤT CẢ client hiển thị hiệu ứng hình ảnh/âm thanh.

        [ClientRpc]
        private void ShowSpeedEffectClientRpc(float duration)
        {
            if (m_PowerUpHUD != null)
                m_PowerUpHUD.SetActivePowerUp(PowerUp.PowerUpType.Speed, duration);
            // Bạn có thể thêm hiệu ứng particle hoặc âm thanh ở đây
        }

        [ClientRpc]
        private void ShowShootingRateEffectClientRpc(float duration)
        {
            if (m_PowerUpHUD != null)
                m_PowerUpHUD.SetActivePowerUp(PowerUp.PowerUpType.ShootingBonus, duration);
        }

        [ClientRpc]
        private void ShowShieldEffectClientRpc(float duration)
        {
            if (m_PowerUpHUD != null)
                m_PowerUpHUD.SetActivePowerUp(PowerUp.PowerUpType.DamageReduction, duration);
        }

        [ClientRpc]
        private void ShowInvincibilityEffectClientRpc(float duration)
        {
            if (m_PowerUpHUD != null)
                m_PowerUpHUD.SetActivePowerUp(PowerUp.PowerUpType.Invincibility, duration);
        }

        [ClientRpc]
        private void ShowHealingEffectClientRpc()
        {
            if (m_PowerUpHUD != null)
                m_PowerUpHUD.SetActivePowerUp(PowerUp.PowerUpType.Healing, 1.0f);
        }

        [ClientRpc]
        private void ShowSpecialShellEffectClientRpc()
        {
            if (m_PowerUpHUD != null)
                m_PowerUpHUD.SetActivePowerUp(PowerUp.PowerUpType.DamageMultiplier, 0f);
        }
        #endregion
    }
}