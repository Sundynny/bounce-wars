using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tanks.Complete
{
    public class TankShooting : MonoBehaviour
    {
        // --- CÁC BIẾN CÀI ĐẶT ---
        [Header("Animation Settings")]
        [Tooltip("Kéo Animator của nhân vật vào đây.")]
        public Animator m_Animator;
        [Tooltip("Thời gian (giây) cần nhấn giữ để kích hoạt animation tấn công mạnh.")]
        public float m_ChargeUpTimeToStrongAttack = 0.5f;

        // --- CÁC BIẾN CŨ GIỮ NGUYÊN ---
        private LineRenderer m_TrajectoryLine;
        private int m_TrajectoryResolution = 30;
        private float m_TrajectoryTimeStep = 0.1f;
        public Rigidbody m_Shell;
        public Transform m_FireTransform;
        public Slider m_AimSlider;
        public AudioSource m_ShootingAudio;
        public AudioClip m_ChargingClip;
        public AudioClip m_FireClip;
        public float m_MinLaunchForce = 5f;
        public float m_MaxLaunchForce = 20f;
        public float m_MaxChargeTime = 0.75f;
        public float m_ShotCooldown = 1.0f;
        [Header("Thuộc tính của đạn")]
        public float m_MaxDamage = 100f;
        public float m_ExplosionForce = 1000f;
        public float m_ExplosionRadius = 5f;

        [HideInInspector]
        public TankInputUser m_InputUser;
        public float CurrentChargeRatio => (m_CurrentLaunchForce - m_MinLaunchForce) / (m_MaxLaunchForce - m_MinLaunchForce);
        public bool IsCharging => m_IsCharging;
        public bool m_IsComputerControlled { get; set; } = false;

        // --- CÁC BIẾN PRIVATE ---
        private float m_CurrentLaunchForce;
        private float m_ChargeSpeed;
        private bool m_Fired;
        private bool m_HasSpecialShell;
        private float m_SpecialShellMultiplier;
        private InputAction fireAction;
        private bool m_IsCharging = false;
        private float m_BaseMinLaunchForce;
        private float m_ShotCooldownTimer;

        // Biến để theo dõi thời gian nhấn giữ cho animation
        private float m_ChargeDuration = 0f;

        // OnEnable được gọi khi GameObject được kích hoạt.
        private void OnEnable()
        {
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_BaseMinLaunchForce = m_MinLaunchForce;
            if (m_AimSlider != null) m_AimSlider.value = m_BaseMinLaunchForce;
            m_HasSpecialShell = false;
            m_SpecialShellMultiplier = 1.0f;
            if (m_AimSlider != null)
            {
                m_AimSlider.minValue = m_MinLaunchForce;
                m_AimSlider.maxValue = m_MaxLaunchForce;
            }
        }

        // Awake được gọi khi script được tải.
        private void Awake()
        {
            m_InputUser = GetComponent<TankInputUser>();
            if (m_InputUser == null) m_InputUser = gameObject.AddComponent<TankInputUser>();
            m_TrajectoryLine = GetComponent<LineRenderer>();
            // Tự động lấy Animator nếu chưa được gán trong Inspector.
            if (m_Animator == null) m_Animator = GetComponentInChildren<Animator>();
        }

        // Start được gọi trước frame đầu tiên.
        private void Start()
        {
            // Gán và kích hoạt hành động "Fire" từ Input System.
            fireAction = m_InputUser.ActionAsset.FindAction("Fire");
            fireAction.Enable();
            // Tính toán tốc độ nạp đạn.
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
        }

        // Update được gọi mỗi frame.
        private void Update()
        {
            if (!m_IsComputerControlled)
            {
                HumanUpdate();
            }
            else
            {
                ComputerUpdate();
            }
        }

        // Hàm cập nhật logic cho người chơi. Đã được sửa đổi theo Phương án B.
        void HumanUpdate()
        {
            // Giảm thời gian hồi chiêu nếu có.
            if (m_ShotCooldownTimer > 0.0f)
            {
                m_ShotCooldownTimer -= Time.deltaTime;
            }

            // Cập nhật thanh trượt ngắm bắn.
            if (m_AimSlider != null) m_AimSlider.value = m_IsCharging ? m_CurrentLaunchForce : m_BaseMinLaunchForce;

            // ---- LOGIC ĐIỀU KHIỂN ANIMATION VÀ TẤN CÔNG (THEO PHƯƠNG ÁN B) ----

            // 1. KHI NGƯỜI CHƠI BẮT ĐẦU NHẤN NÚT
            if (m_ShotCooldownTimer <= 0 && fireAction.WasPressedThisFrame())
            {
                // Bắt đầu quá trình nạp đạn.
                m_IsCharging = true;
                m_Fired = false;
                m_CurrentLaunchForce = m_MinLaunchForce;
                m_ChargeDuration = 0f;

                // Phát âm thanh nạp đạn.
                m_ShootingAudio.clip = m_ChargingClip;
                m_ShootingAudio.Play();

                // KÍCH HOẠT "CỔNG VÀO" TẤN CÔNG
                // Gửi trigger "shoot" để bắt đầu animation tấn công thường.
                // Animation này sẽ tự động quay về Idle nhờ "Has Exit Time".
                if (m_Animator != null) m_Animator.SetTrigger("shoot");
            }
            // 2. TRONG KHI NGƯỜI CHƠI ĐANG NHẤN GIỮ
            else if (fireAction.IsPressed() && m_IsCharging && !m_Fired)
            {
                // Tăng lực bắn và thời gian đã nhấn giữ.
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
                m_ChargeDuration += Time.deltaTime;

                // KHI NHẤN GIỮ ĐỦ LÂU, CHUYỂN SANG TẤN CÔNG MẠNH
                // Đặt abilityIndex = 2 để ra lệnh cho Animator chuyển từ animation thường
                // sang animation tấn công mạnh (dạng lặp).
                if (m_ChargeDuration >= m_ChargeUpTimeToStrongAttack)
                {
                    if (m_Animator != null) m_Animator.SetInteger("abilityIndex", 2);
                }

                // Tự động bắn khi nạp đầy.
                if (m_CurrentLaunchForce >= m_MaxLaunchForce)
                {
                    m_CurrentLaunchForce = m_MaxLaunchForce;
                    Fire();
                }
            }
            // 3. KHI NGƯỜI CHƠI THẢ NÚT
            else if (fireAction.WasReleasedThisFrame() && !m_Fired)
            {
                Fire();
            }

            // Vẽ đường đạn.
            if (m_IsCharging && !m_Fired && m_TrajectoryLine != null)
            {
                ShowTrajectory(m_CurrentLaunchForce);
            }
            else if (m_TrajectoryLine != null)
            {
                m_TrajectoryLine.positionCount = 0;
            }
        }

        // Hàm bắn đạn.
        private void Fire()
        {
            if (!m_IsCharging) return;

            m_Fired = true;
            m_IsCharging = false;

            // RESET ANIMATION TẤN CÔNG MẠNH
            // Đặt abilityIndex về 0. Lệnh này chủ yếu dùng để phá vỡ vòng lặp của
            // animation tấn công mạnh, ra lệnh cho nó quay về Idle.
            if (m_Animator != null)
            {
                m_Animator.SetInteger("abilityIndex", 0);
            }

            // Logic tạo và bắn đạn.
            Rigidbody shellInstance = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;
            shellInstance.linearVelocity = m_CurrentLaunchForce * m_FireTransform.forward;

            ShellExplosion explosionData = shellInstance.GetComponent<ShellExplosion>();
            if (explosionData != null)
            {
                explosionData.m_ExplosionForce = m_ExplosionForce;
                explosionData.m_ExplosionRadius = m_ExplosionRadius;
                explosionData.m_MaxDamage = m_MaxDamage;
            }

            if (m_HasSpecialShell)
            {
                if (explosionData != null)
                    explosionData.m_MaxDamage *= m_SpecialShellMultiplier;
                m_HasSpecialShell = false;
                m_SpecialShellMultiplier = 1f;
                PowerUpHUD powerUpHUD = GetComponentInChildren<PowerUpHUD>();
                if (powerUpHUD != null)
                    powerUpHUD.DisableActiveHUD();
            }

            if (m_ShootingAudio != null && m_FireClip != null)
            {
                m_ShootingAudio.clip = m_FireClip;
                m_ShootingAudio.Play();
            }

            m_CurrentLaunchForce = m_MinLaunchForce;
            m_ShotCooldownTimer = m_ShotCooldown;
        }

        // --- CÁC HÀM CŨ KHÁC GIỮ NGUYÊN ---

        public void StartCharging()
        {
            m_IsCharging = true;
            m_Fired = false;
            m_CurrentLaunchForce = m_MinLaunchForce;

            m_ShootingAudio.clip = m_ChargingClip;
            m_ShootingAudio.Play();
        }

        public void StopCharging()
        {
            if (m_IsCharging)
            {
                Fire();
                m_IsCharging = false;
            }
        }

        void ComputerUpdate()
        {
            if (m_AimSlider != null) m_AimSlider.value = m_BaseMinLaunchForce;

            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                m_CurrentLaunchForce = m_MaxLaunchForce;
                Fire();
            }
            else if (m_IsCharging && !m_Fired)
            {
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
                if (m_AimSlider != null) m_AimSlider.value = m_CurrentLaunchForce;
            }
            else if (fireAction.WasReleasedThisFrame() && !m_Fired)
            {
                Fire();
                m_IsCharging = false;
            }
            if (m_IsCharging && !m_Fired && m_TrajectoryLine != null)
            {
                ShowTrajectory(m_CurrentLaunchForce);
            }
            else if (m_TrajectoryLine != null)
            {
                m_TrajectoryLine.positionCount = 0;
            }
        }

        private void ShowTrajectory(float launchForce)
        {
            if (m_TrajectoryLine == null) return;
            Vector3[] points = new Vector3[m_TrajectoryResolution];
            Vector3 startPos = m_FireTransform.position;
            Vector3 startVelocity = m_FireTransform.forward * launchForce;

            for (int i = 0; i < m_TrajectoryResolution; i++)
            {
                float t = i * m_TrajectoryTimeStep;
                Vector3 point = startPos + startVelocity * t + 0.5f * Physics.gravity * t * t;
                points[i] = point;
            }

            m_TrajectoryLine.positionCount = m_TrajectoryResolution;
            m_TrajectoryLine.SetPositions(points);
        }

        public void EquipSpecialShell(float damageMultiplier)
        {
            m_HasSpecialShell = true;
            m_SpecialShellMultiplier = damageMultiplier;
        }

        public Vector3 GetProjectilePosition(float chargingLevel)
        {
            float chargeLevel = Mathf.Lerp(m_MinLaunchForce, m_MaxLaunchForce, chargingLevel);
            Vector3 velocity = chargeLevel * m_FireTransform.forward;

            float a = 0.5f * Physics.gravity.y;
            float b = velocity.y;
            float c = m_FireTransform.position.y;

            float sqrtContent = b * b - 4 * a * c;

            if (sqrtContent <= 0)
            {
                return m_FireTransform.position;
            }

            float answer1 = (-b + Mathf.Sqrt(sqrtContent)) / (2 * a);
            float answer2 = (-b - Mathf.Sqrt(sqrtContent)) / (2 * a);

            float answer = answer1 > 0 ? answer1 : answer2;

            Vector3 position = m_FireTransform.position +
                               new Vector3(velocity.x, 0, velocity.z) *
                               answer;
            position.y = 0;

            return position;
        }
    }
}