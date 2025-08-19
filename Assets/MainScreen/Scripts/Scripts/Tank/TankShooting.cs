using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tanks.Complete
{
    public class TankShooting : MonoBehaviour
    {
        private LineRenderer m_TrajectoryLine;
        private int m_TrajectoryResolution = 30;
        private float m_TrajectoryTimeStep = 0.1f;

        public Rigidbody m_Shell;                   // Prefab của quả đạn.
        public Transform m_FireTransform;           // Một đối tượng con của xe tăng, nơi đạn được sinh ra.
        public Slider m_AimSlider;                  // Một đối tượng con của xe tăng, hiển thị lực bắn hiện tại.
        public AudioSource m_ShootingAudio;         // Tham chiếu đến nguồn âm thanh được sử dụng để phát âm thanh bắn. Lưu ý: khác với nguồn âm thanh di chuyển.
        public AudioClip m_ChargingClip;            // Âm thanh phát ra khi mỗi phát bắn đang được nạp.
        public AudioClip m_FireClip;                // Âm thanh phát ra khi mỗi phát bắn được bắn đi.
        [Tooltip("Tốc độ (đơn vị/giây) của quả đạn khi được bắn ở mức nạp tối thiểu")]
        public float m_MinLaunchForce = 5f;        // Lực tác động lên quả đạn nếu nút bắn không được giữ.
        [Tooltip("Tốc độ (đơn vị/giây) của quả đạn khi được bắn ở mức nạp tối đa")]
        public float m_MaxLaunchForce = 20f;        // Lực tác động lên quả đạn nếu nút bắn được giữ trong thời gian nạp tối đa.
        [Tooltip("Thời gian nạp tối đa. Khi thời gian nạp đạt đến mức này, quả đạn sẽ được bắn với Lực bắn tối đa (MaxLaunchForce)")]
        public float m_MaxChargeTime = 0.75f;       // Thời gian mà quả đạn có thể nạp trước khi được bắn với lực tối đa.
        [Tooltip("Thời gian phải trôi qua trước khi có thể bắn lại sau một phát bắn")]
        public float m_ShotCooldown = 1.0f;         // Thời gian cần thiết giữa 2 phát bắn
        [Header("Thuộc tính của đạn")]
        [Tooltip("Lượng máu bị trừ của một xe tăng nếu chúng ở ngay tại điểm rơi của quả đạn")]
        public float m_MaxDamage = 100f;                    // Lượng sát thương gây ra nếu vụ nổ có tâm điểm là một chiếc xe tăng.
        [Tooltip("Lực của vụ nổ tại vị trí quả đạn. Đơn vị là newton, vì vậy cần phải cao, hãy giữ nó ở mức 500 trở lên")]
        public float m_ExplosionForce = 1000f;              // Lượng lực tác động thêm vào một chiếc xe tăng ở tâm vụ nổ.
        [Tooltip("Bán kính của vụ nổ tính bằng đơn vị Unity. Lực giảm dần theo khoảng cách đến tâm, và một chiếc xe tăng ở xa hơn khoảng cách này so với vụ nổ của đạn sẽ không bị ảnh hưởng bởi vụ nổ")]
        public float m_ExplosionRadius = 5f;                // Khoảng cách tối đa so với vụ nổ mà xe tăng vẫn có thể bị ảnh hưởng.

        [HideInInspector]
        public TankInputUser m_InputUser;           // Thành phần Người dùng Đầu vào (Input User) cho xe tăng đó. Chứa các Hành động Đầu vào (Input Actions). 

        public float CurrentChargeRatio =>
            (m_CurrentLaunchForce - m_MinLaunchForce) / (m_MaxLaunchForce - m_MinLaunchForce); //Mức độ nạp đạn trong khoảng 0-1
        public bool IsCharging => m_IsCharging;

        public bool m_IsComputerControlled { get; set; } = false;

        private string m_FireButton;                // Trục đầu vào được sử dụng để phóng đạn.
        private float m_CurrentLaunchForce;         // Lực sẽ được truyền cho quả đạn khi nút bắn được thả ra.
        private float m_ChargeSpeed;                // Tốc độ tăng lực bắn, dựa trên thời gian nạp tối đa.
        private bool m_Fired;                       // Quả đạn đã được phóng đi với lần nhấn nút này hay chưa.
        private bool m_HasSpecialShell;             // xe tăng có quả đạn gây thêm sát thương không?
        private float m_SpecialShellMultiplier;     // Lượng mà quả đạn đặc biệt sẽ nhân sát thương.
        private InputAction fireAction;             // Hành động Đầu vào (Input Action) để bắn, lấy từ TankInputUser
        private bool m_IsCharging = false;          // Chúng ta có đang nạp đạn không
        private float m_BaseMinLaunchForce;         // Giá trị ban đầu của m_MinLaunchForce
        private float m_ShotCooldownTimer;          // Bộ đếm thời gian đếm ngược trước khi được phép bắn lại

        private void OnEnable()
        {
            // Khi xe tăng được bật, đặt lại lực bắn, giao diện người dùng và các vật phẩm tăng sức mạnh
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_BaseMinLaunchForce = m_MinLaunchForce;
            m_AimSlider.value = m_BaseMinLaunchForce;
            m_HasSpecialShell = false;
            m_SpecialShellMultiplier = 1.0f;

            m_AimSlider.minValue = m_MinLaunchForce;
            m_AimSlider.maxValue = m_MaxLaunchForce;
        }

        private void Awake()
        {
            m_InputUser = GetComponent<TankInputUser>();
            if (m_InputUser == null)
                m_InputUser = gameObject.AddComponent<TankInputUser>();
            m_TrajectoryLine = GetComponent<LineRenderer>();
        }

        private void Start()
        {
            // Trục bắn được dựa trên số của người chơi.
            m_FireButton = "Fire";
            fireAction = m_InputUser.ActionAsset.FindAction(m_FireButton);

            fireAction.Enable();

            // Tốc độ tăng của lực bắn là phạm vi các lực có thể có chia cho thời gian nạp tối đa.
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
        }


        private void Update()
        {
            // Xe tăng do Máy tính và Người chơi điều khiển sử dụng 2 hàm cập nhật khác nhau
            if (!m_IsComputerControlled)
            {
                HumanUpdate();
            }
            else
            {
                ComputerUpdate();
            }
        }

        /// <summary>
        /// Được AI sử dụng để bắt đầu nạp đạn
        /// </summary>
        public void StartCharging()
        {
            m_IsCharging = true;
            // ... đặt lại cờ đã bắn và đặt lại lực bắn.
            m_Fired = false;
            m_CurrentLaunchForce = m_MinLaunchForce;

            // Thay đổi clip âm thanh thành clip nạp đạn và bắt đầu phát.
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
            // Thanh trượt nên có giá trị mặc định là lực bắn tối thiểu.
            m_AimSlider.value = m_BaseMinLaunchForce;

            // Nếu lực tối đa đã bị vượt quá và quả đạn vẫn chưa được phóng đi...
            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                // ... sử dụng lực tối đa và phóng quả đạn.
                m_CurrentLaunchForce = m_MaxLaunchForce;
                Fire();
            }
            // Ngược lại, nếu nút bắn đang được giữ và quả đạn vẫn chưa được phóng đi...
            else if (m_IsCharging && !m_Fired)
            {
                // Tăng lực bắn và cập nhật thanh trượt.
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;

                m_AimSlider.value = m_CurrentLaunchForce;
            }
            // Ngược lại, nếu nút bắn được thả ra và quả đạn vẫn chưa được phóng đi...
            else if (fireAction.WasReleasedThisFrame() && !m_Fired)
            {
                // ... phóng quả đạn.
                Fire();
                m_IsCharging = false;
            }
            if (m_IsCharging && !m_Fired)
            {
                ShowTrajectory(m_CurrentLaunchForce);
            }
            else
            {
                m_TrajectoryLine.positionCount = 0;
            }
        }

        void HumanUpdate()
        {
            // nếu có bộ đếm thời gian hồi chiêu, hãy giảm nó đi
            if (m_ShotCooldownTimer > 0.0f)
            {
                m_ShotCooldownTimer -= Time.deltaTime;
            }

            // Thanh trượt nên có giá trị mặc định là lực bắn tối thiểu.
            m_AimSlider.value = m_BaseMinLaunchForce;

            // Nếu lực tối đa đã bị vượt quá và quả đạn vẫn chưa được phóng đi...
            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                // ... sử dụng lực tối đa và phóng quả đạn.
                m_CurrentLaunchForce = m_MaxLaunchForce;
                Fire();
            }
            // Ngược lại, nếu nút bắn vừa mới bắt đầu được nhấn...
            else if (m_ShotCooldownTimer <= 0 && fireAction.WasPressedThisFrame())
            {
                // ... đặt lại cờ đã bắn và đặt lại lực bắn.
                m_Fired = false;
                m_CurrentLaunchForce = m_MinLaunchForce;

                // Thay đổi clip âm thanh thành clip nạp đạn và bắt đầu phát.
                m_ShootingAudio.clip = m_ChargingClip;
                m_ShootingAudio.Play();
            }
            // Ngược lại, nếu nút bắn đang được giữ và quả đạn vẫn chưa được phóng đi...
            else if (fireAction.IsPressed() && !m_Fired)
            {
                // Tăng lực bắn và cập nhật thanh trượt.
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;

                m_AimSlider.value = m_CurrentLaunchForce;
            }
            // Ngược lại, nếu nút bắn được thả ra và quả đạn vẫn chưa được phóng đi...
            else if (fireAction.WasReleasedThisFrame() && !m_Fired)
            {
                // ... phóng quả đạn.
                Fire();
            }
            if (fireAction.IsPressed() && !m_Fired)
            {
                ShowTrajectory(m_CurrentLaunchForce);
            }
            else
            {
                m_TrajectoryLine.positionCount = 0; // Ẩn khi không bắn
            }
        }

        private void ShowTrajectory(float launchForce)
        {
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



        private void Fire()
        {
            m_Fired = true;

            Rigidbody shellInstance =
                Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;

            shellInstance.linearVelocity = m_CurrentLaunchForce * m_FireTransform.forward;

            ShellExplosion explosionData = shellInstance.GetComponent<ShellExplosion>();
            if (explosionData != null)
            {
                explosionData.m_ExplosionForce = m_ExplosionForce;
                explosionData.m_ExplosionRadius = m_ExplosionRadius;
                explosionData.m_MaxDamage = m_MaxDamage;
            }

            // --- PHẦN SỬA LỖI ---
            if (m_HasSpecialShell)
            {
                if (explosionData != null)
                    explosionData.m_MaxDamage *= m_SpecialShellMultiplier;

                m_HasSpecialShell = false;
                m_SpecialShellMultiplier = 1f;

                // Đoạn mã cũ gây lỗi đã được XÓA.
                // Logic này không còn cần thiết vì hệ thống PowerUpDetector mới không sử dụng biến m_HasActivePowerUp.

                PowerUpHUD powerUpHUD = GetComponentInChildren<PowerUpHUD>();
                if (powerUpHUD != null)
                    powerUpHUD.DisableActiveHUD();
            }
            // --- KẾT THÚC PHẦN SỬA LỖI ---

            if (m_ShootingAudio != null && m_FireClip != null)
            {
                m_ShootingAudio.clip = m_FireClip;
                m_ShootingAudio.Play();
            }

            m_CurrentLaunchForce = m_MinLaunchForce;
            m_ShotCooldownTimer = m_ShotCooldown;
        }

        public void EquipSpecialShell(float damageMultiplier)
        {
            m_HasSpecialShell = true;
            m_SpecialShellMultiplier = damageMultiplier;
        }

        /// <summary>
        /// Trả về vị trí ước tính mà đạn sẽ có với mức độ nạp (từ 0 đến 1)
        /// </summary>
        /// <param name="chargingLevel">Mức độ nạp bắn từ 0 - 1</param>
        /// <returns>Vị trí mà đạn sẽ ở (bỏ qua chướng ngại vật)</returns>
        public Vector3 GetProjectilePosition(float chargingLevel)
        {
            float chargeLevel = Mathf.Lerp(m_MinLaunchForce, m_MaxLaunchForce, chargingLevel);
            Vector3 velocity = chargeLevel * m_FireTransform.forward;

            float a = 0.5f * Physics.gravity.y;
            float b = velocity.y;
            float c = m_FireTransform.position.y;

            float sqrtContent = b * b - 4 * a * c;
            //không có giải pháp
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