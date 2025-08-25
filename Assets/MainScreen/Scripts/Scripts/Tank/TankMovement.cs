using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Tanks.Complete
{
    // [DefaultExecutionOrder(-10)]
    // Đảm bảo script này chạy trước các script khác để chúng có thể lấy tham chiếu từ đây một cách an toàn.
    [DefaultExecutionOrder(-10)]
    public class TankMovement : MonoBehaviour
    {
        // --- CÁC BIẾN CÀI ĐẶT ---
        [Header("Camera Reference")]
        [Tooltip("Kéo Camera của người chơi này vào đây. Rất quan trọng cho game split-screen!")]
        public Camera m_PlayerCamera;

        [Header("Character Style Movement")]
        [Tooltip("Thời gian (giây) để nhân vật xoay mượt mà về hướng mới. Càng nhỏ càng xoay nhanh.")]
        public float m_RotationSmoothTime = 0.1f;

        [Header("Original Settings (Kept for Compatibility)")]
        [Tooltip("Tốc độ di chuyển cơ bản của nhân vật (mét/giây).")]
        public float m_Speed = 6f; // Vẫn được sử dụng trong logic di chuyển mới.
        [Tooltip("Tốc độ xoay cũ (không còn dùng nhưng giữ lại để không gây lỗi).")]
        public float m_TurnSpeed = 180f; // Không còn được sử dụng trong logic mới.
        [Tooltip("Chế độ điều khiển trực tiếp cũ (logic mới mặc định là chế độ này).")]
        public bool m_IsDirectControl; // Không còn được sử dụng trong logic mới.

        [Header("Jump Settings")]
        [Tooltip("Lực đẩy nhân vật lên khi nhảy.")]
        public float m_JumpForce = 7f;
        [Tooltip("LayerMask để xác định đâu là 'mặt đất'.")]
        public LayerMask m_GroundMask;
        [Tooltip("Vị trí dùng để kiểm tra xem nhân vật có đang chạm đất không.")]
        public Transform m_GroundCheck;
        [Tooltip("Bán kính của vòng tròn kiểm tra chạm đất.")]
        public float m_GroundCheckRadius = 0.3f;

        [Tooltip("Thời gian (giây) nhân vật phải rời đất trước khi animation rơi được kích hoạt. Giúp chống lỗi do địa hình gồ ghề.")]
        public float m_GroundCheckDelay = 0.1f;

        [Header("Audio & Effects")]
        public AudioSource m_MovementAudio;
        public AudioClip m_EngineIdling;
        public AudioClip m_EngineDriving;
        [Tooltip("Khoảng dao động cao độ ngẫu nhiên của âm thanh để tránh bị lặp lại nhàm chán.")]
        public float m_PitchRange = 0.2f;

        [Header("Player Setup")]
        public int m_PlayerNumber = 1;
        public bool m_IsComputerControlled = false;

        [HideInInspector]
        public TankInputUser m_InputUser; // Giữ nguyên để các script khác có thể tham chiếu.
        public Rigidbody Rigidbody => m_Rigidbody;
        public int ControlIndex { get; set; } = -1;

        // --- CÁC BIẾN NỘI BỘ (PRIVATE) ---
        private Rigidbody m_Rigidbody;
        private Animator m_Animator;

        // Biến cho Input System
        private float m_MovementInputValue;
        private float m_TurnInputValue;
        private InputAction m_MoveAction;
        private InputAction m_TurnAction;
        private InputAction m_JumpAction;
        private Vector3 m_InputDirection;   // Vector lưu hướng di chuyển mong muốn

        // Biến cho Audio & Effects
        private float m_OriginalPitch;
        private ParticleSystem[] m_particleSystems;

        // Biến cho Logic trạng thái
        private bool m_IsGrounded = false;
        private bool m_IsHasted = false;
        private float m_TurnSmoothVelocity; // Biến phụ cho việc xoay mượt

        // Biến cho các giải pháp mới
        private float m_TimeSinceLeftGround = 0f; // Bộ đếm thời gian từ khi rời đất
        private Vector3 m_LastPosition; // Vị trí ở frame trước để tính vận tốc
        private float m_CurrentSpeed; // Vận tốc thực tế hiện tại


        // Awake được gọi khi script instance được tải. Dùng để khởi tạo tham chiếu.
        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_InputUser = GetComponent<TankInputUser>();
            if (m_InputUser == null)
                m_InputUser = gameObject.AddComponent<TankInputUser>();

            m_Animator = GetComponentInChildren<Animator>();
        }

        // OnEnable được gọi mỗi khi GameObject được kích hoạt. Dùng để reset trạng thái.
        private void OnEnable()
        {
            m_Rigidbody.isKinematic = false;
            m_MovementInputValue = 0f;
            m_TurnInputValue = 0f;
            m_particleSystems = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < m_particleSystems.Length; ++i)
                m_particleSystems[i].Play();
        }

        // OnDisable được gọi mỗi khi GameObject bị vô hiệu hóa.
        private void OnDisable()
        {
            m_Rigidbody.isKinematic = true;
            for (int i = 0; i < m_particleSystems.Length; ++i)
                m_particleSystems[i].Stop();
        }

        // Start được gọi trước frame đầu tiên. Dùng để thiết lập logic.
        private void Start()
        {
            // Thiết lập Input System cho AI, PC hoặc Mobile.
            if (m_IsComputerControlled)
            {
                var ai = GetComponent<TankAI>();
                if (ai == null) { gameObject.AddComponent<TankAI>(); }
            }
            if (ControlIndex == -1 && !m_IsComputerControlled) { ControlIndex = m_PlayerNumber; }
            var mobileControl = FindAnyObjectByType<MobileUIControl>();
            if (mobileControl != null && ControlIndex == 1)
            {
                m_InputUser.SetNewInputUser(InputUser.PerformPairingWithDevice(mobileControl.Device));
                m_InputUser.ActivateScheme("Gamepad");
            }
            else
            {
                m_InputUser.ActivateScheme(ControlIndex == 1 ? "KeyboardLeft" : "KeyboardRight");
            }
            // Gán và kích hoạt các hành động input.
            m_MoveAction = m_InputUser.ActionAsset.FindAction("Vertical");
            m_TurnAction = m_InputUser.ActionAsset.FindAction("Horizontal");
            m_JumpAction = m_InputUser.ActionAsset.FindAction("Jump");
            m_MoveAction?.Enable();
            m_TurnAction?.Enable();
            m_JumpAction?.Enable();
            if (m_MovementAudio != null) m_OriginalPitch = m_MovementAudio.pitch;

            // Khởi tạo vị trí ban đầu để tính vận tốc.
            m_LastPosition = transform.position;
        }

        // Update được gọi mỗi frame. Dùng để đọc input và cập nhật các trạng thái logic không liên quan vật lý.
        private void Update()
        {
            if (!m_IsComputerControlled)
            {
                // Đọc input từ người chơi.
                m_MovementInputValue = m_MoveAction.ReadValue<float>();
                m_TurnInputValue = m_TurnAction.ReadValue<float>();
                
                // Tạo vector hướng chuẩn hóa từ input.
                m_InputDirection = new Vector3(m_TurnInputValue, 0f, m_MovementInputValue).normalized;
            }

            // Gọi các hàm kiểm tra trạng thái và hành động.
            CheckGrounded();
            HandleJump();
            EngineAudio();
            UpdateAnimator();
        }

        // FixedUpdate được gọi theo một chu kỳ thời gian cố định. Dùng để xử lý vật lý.
        private void FixedUpdate()
        {
            // Tính toán vận tốc thực tế của Rigidbody.
            float distanceMoved = Vector3.Distance(transform.position, m_LastPosition);
            m_CurrentSpeed = distanceMoved / Time.fixedDeltaTime; // Vận tốc = Quãng đường / Thời gian
            m_LastPosition = transform.position; // Cập nhật vị trí cuối cùng cho lần tính tiếp theo.

            // Thực hiện di chuyển và xoay.
            HandleMovementAndRotation();
        }

        // Hàm xử lý di chuyển và xoay kiểu nhân vật.
        private void HandleMovementAndRotation()
        {
            // Kiểm tra an toàn: Dừng lại nếu quên gán camera.
            if (m_PlayerCamera == null)
            {
                Debug.LogError("Player Camera chưa được gán trong Inspector của nhân vật: " + gameObject.name);
                return;
            }
            // Chỉ thực hiện khi có input.
            if (m_InputDirection.magnitude < 0.1f) return;

            // Tính toán góc xoay mong muốn dựa trên hướng input của người chơi và góc quay của camera.
            float targetAngle = Mathf.Atan2(m_InputDirection.x, m_InputDirection.z) * Mathf.Rad2Deg + m_PlayerCamera.transform.eulerAngles.y;

            // Xoay nhân vật một cách mượt mà về góc mục tiêu bằng SmoothDampAngle.
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref m_TurnSmoothVelocity, m_RotationSmoothTime);
            m_Rigidbody.MoveRotation(Quaternion.Euler(0f, angle, 0f));

            // Di chuyển nhân vật về phía nó đang nhìn.
            Vector3 moveDirection = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            m_Rigidbody.MovePosition(m_Rigidbody.position + moveDirection.normalized * m_Speed * Time.fixedDeltaTime);
        }

        // Hàm công khai để script khác (như PowerUpDetector) có thể bật/tắt trạng thái Haste.
        public void SetHasteStatus(bool hasHaste)
        {
            m_IsHasted = hasHaste;
        }

        // Hàm tập trung xử lý việc gửi thông tin đến Animator.
        private void UpdateAnimator()
        {
            if (m_Animator == null) return; // Kiểm tra an toàn.

            // 1. CHỐNG NHẢY GIẢ: Chỉ coi là "đang ở trên không" (isJumping) nếu đã rời đất một khoảng thời gian đủ lâu.
            bool isActuallyInAir = m_TimeSinceLeftGround > m_GroundCheckDelay;
            m_Animator.SetBool("isJumping", isActuallyInAir);

            // 2. KÍCH HOẠT HASTE DỰA TRÊN TỐC ĐỘ THỰC:
            float hasteSpeedThreshold = m_Speed * 1.5f; // Ngưỡng để bật animation Haste.
            // Điều kiện: phải đang di chuyển VÀ tốc độ thực tế phải vượt ngưỡng.
            bool shouldShowHasteAnimation = m_CurrentSpeed > hasteSpeedThreshold && m_InputDirection.magnitude > 0.1f;
            m_Animator.SetBool("isHaste", shouldShowHasteAnimation);

            // 3. LOGIC DI CHUYỂN CƠ BẢN:
            m_Animator.SetBool("isMoving", m_InputDirection.magnitude >= 0.1f);
        }

        // Hàm kiểm tra chạm đất được nâng cấp với bộ đếm thời gian.
        private void CheckGrounded()
        {
            if (m_GroundCheck != null)
            {
                // Dùng Physics.CheckSphere để kiểm tra va chạm với mặt đất.
                m_IsGrounded = Physics.CheckSphere(m_GroundCheck.position, m_GroundCheckRadius, m_GroundMask);

                // Cập nhật bộ đếm thời gian.
                if (m_IsGrounded)
                {
                    m_TimeSinceLeftGround = 0f; // Nếu chạm đất, reset bộ đếm.
                }
                else
                {
                    m_TimeSinceLeftGround += Time.deltaTime; // Nếu không chạm đất, bắt đầu đếm.
                }
            }
        }

        // Hàm xử lý hành động nhảy.
        private void HandleJump()
        {
            // Chỉ nhảy khi có input và đang trên mặt đất.
            if (m_JumpAction != null && m_JumpAction.WasPressedThisFrame() && m_IsGrounded)
            {
                m_Rigidbody.AddForce(Vector3.up * m_JumpForce, ForceMode.Impulse);
            }
        }

        // Hàm xử lý âm thanh động cơ.
        private void EngineAudio()
        {
            if (m_MovementAudio == null) return;

            // Kiểm tra nếu nhân vật đang di chuyển.
            if (m_InputDirection.magnitude >= 0.1f)
            {
                // Nếu âm thanh đang phát là tiếng đứng yên...
                if (m_MovementAudio.clip == m_EngineIdling)
                {
                    // ...thì chuyển sang âm thanh di chuyển và phát.
                    m_MovementAudio.clip = m_EngineDriving;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
            else // Nếu nhân vật đang đứng yên.
            {
                // Nếu âm thanh đang phát là tiếng di chuyển...
                if (m_MovementAudio.clip == m_EngineDriving)
                {
                    // ...thì chuyển sang âm thanh đứng yên và phát.
                    m_MovementAudio.clip = m_EngineIdling;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
        }
    }
}