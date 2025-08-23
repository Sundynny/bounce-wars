using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic; // Cần dùng để có List

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

        [Header("Shell Prefabs")]
        [Tooltip("Prefab viên đạn thường, bắn khi không có nguyên tố nào.")]
        public Rigidbody m_NormalShell;
        [Tooltip("Prefab viên đạn Lửa.")]
        public Rigidbody m_FireShell;
        [Tooltip("Prefab viên đạn Nước.")]
        public Rigidbody m_WaterShell;
        [Tooltip("Prefab viên đạn Gió.")]
        public Rigidbody m_WindShell;
        [Tooltip("Prefab viên đạn Đất.")]
        public Rigidbody m_EarthShell;

        [Header("Component References")]
        [Tooltip("Tham chiếu đến script OrbCollector trên cùng nhân vật.")]
        public OrbCollector m_OrbCollector;

        // --- CÁC BIẾN CŨ GIỮ NGUYÊN ---
        private LineRenderer m_TrajectoryLine;
        private int m_TrajectoryResolution = 30;
        private float m_TrajectoryTimeStep = 0.1f;
        public Transform m_FireTransform;
        public Slider m_AimSlider;
        public AudioSource m_ShootingAudio;
        public AudioClip m_ChargingClip;
        public AudioClip m_FireClip;
        public float m_MinLaunchForce = 5f;
        public float m_MaxLaunchForce = 20f;
        public float m_MaxChargeTime = 0.75f;
        public float m_ShotCooldown = 1.0f;
        [Header("Base Projectile Properties")]
        public float m_MaxDamage = 100f;
        public float m_ExplosionForce = 1000f;
        public float m_ExplosionRadius = 5f;

        [HideInInspector] public TankInputUser m_InputUser;
        public float CurrentChargeRatio => (m_CurrentLaunchForce - m_MinLaunchForce) / (m_MaxLaunchForce - m_MinLaunchForce);
        public bool IsCharging => m_IsCharging;
        public bool m_IsComputerControlled { get; set; } = false;

        // --- CÁC BIẾN PRIVATE ---
        private float m_CurrentLaunchForce;
        private float m_ChargeSpeed;
        private bool m_Fired;
        private InputAction fireAction;
        private bool m_IsCharging = false;
        private float m_BaseMinLaunchForce;
        private float m_ShotCooldownTimer;
        private float m_ChargeDuration = 0f;

        // Biến HasSpecialShell cũ không còn được dùng theo cách mới
        private bool m_HasSpecialShell;
        private float m_SpecialShellMultiplier;

        private void Awake()
        {
            m_InputUser = GetComponent<TankInputUser>();
            if (m_InputUser == null) m_InputUser = gameObject.AddComponent<TankInputUser>();
            m_TrajectoryLine = GetComponent<LineRenderer>();
            if (m_Animator == null) m_Animator = GetComponentInChildren<Animator>();

            if (m_OrbCollector == null) m_OrbCollector = GetComponent<OrbCollector>();
        }

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

        private void Start()
        {
            fireAction = m_InputUser.ActionAsset.FindAction("Fire");
            fireAction.Enable();
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
        }

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

        void HumanUpdate()
        {
            if (m_ShotCooldownTimer > 0.0f)
            {
                m_ShotCooldownTimer -= Time.deltaTime;
            }

            if (m_AimSlider != null) m_AimSlider.value = m_IsCharging ? m_CurrentLaunchForce : m_BaseMinLaunchForce;

            if (m_ShotCooldownTimer <= 0 && fireAction.WasPressedThisFrame())
            {
                m_IsCharging = true;
                m_Fired = false;
                m_CurrentLaunchForce = m_MinLaunchForce;
                m_ChargeDuration = 0f;
                m_ShootingAudio.clip = m_ChargingClip;
                m_ShootingAudio.Play();
                if (m_Animator != null) m_Animator.SetTrigger("shoot");
            }
            else if (fireAction.IsPressed() && m_IsCharging && !m_Fired)
            {
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
                m_ChargeDuration += Time.deltaTime;
                if (m_ChargeDuration >= m_ChargeUpTimeToStrongAttack)
                {
                    if (m_Animator != null) m_Animator.SetInteger("abilityIndex", 2);
                }
                if (m_CurrentLaunchForce >= m_MaxLaunchForce)
                {
                    m_CurrentLaunchForce = m_MaxLaunchForce;
                    Fire();
                }
            }
            else if (fireAction.WasReleasedThisFrame() && !m_Fired)
            {
                Fire();
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

        // --- HÀM Fire() ĐÃ ĐƯỢC ĐẠI TU VÀ SỬA LỖI ---
        private void Fire()
        {
            if (!m_IsCharging) return;

            m_Fired = true;
            m_IsCharging = false;

            if (m_Animator != null)
            {
                m_Animator.SetInteger("abilityIndex", 0);
            }

            Rigidbody shellToFire = m_NormalShell;

            // SỬA LỖI: Khai báo List với đúng kiểu dữ liệu từ PowerOrbController
            List<PowerOrbController.ElementType> collectedElements = new List<PowerOrbController.ElementType>();

            if (m_OrbCollector != null && m_OrbCollector.IsCarryingAnyOrb)
            {
                collectedElements = m_OrbCollector.GetCollectedOrbTypes();

                if (collectedElements.Count > 0)
                {
                    // SỬA LỖI: So sánh với đúng kiểu enum PowerOrbController.ElementType
                    switch (collectedElements[0])
                    {
                        case PowerOrbController.ElementType.Fire: shellToFire = m_FireShell; break;
                        case PowerOrbController.ElementType.Water: shellToFire = m_WaterShell; break;
                        case PowerOrbController.ElementType.Wind: shellToFire = m_WindShell; break;
                        case PowerOrbController.ElementType.Earth: shellToFire = m_EarthShell; break;
                        default: shellToFire = m_NormalShell; break;
                    }
                }
            }

            Rigidbody shellInstance = Instantiate(shellToFire, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;
            shellInstance.linearVelocity = m_CurrentLaunchForce * m_FireTransform.forward;

            ShellExplosion explosionData = shellInstance.GetComponent<ShellExplosion>();
            if (explosionData != null)
            {
                explosionData.m_Owner = this.gameObject;
                explosionData.m_MaxDamage = m_MaxDamage;
                explosionData.m_ExplosionForce = m_ExplosionForce;
                explosionData.m_ExplosionRadius = m_ExplosionRadius;

                // SỬA LỖI: Chuyển đổi từ List<PowerOrbController.ElementType> sang List<ShellExplosion.ElementType>
                explosionData.m_ActiveElements.Clear();
                foreach (var orbType in collectedElements)
                {
                    // Chuyển đổi (cast) từng enum một. Điều này an toàn vì chúng có cùng thứ tự và giá trị.
                    explosionData.m_ActiveElements.Add((ShellExplosion.ElementType)orbType);
                }
            }

            if (m_OrbCollector != null)
            {
                m_OrbCollector.ConsumeOrbs();
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