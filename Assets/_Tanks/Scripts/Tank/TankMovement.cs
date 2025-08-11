using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using Unity.Netcode; // THÊM MỚI

namespace Tanks.Complete
{
    [DefaultExecutionOrder(-10)]
    public class TankMovement : NetworkBehaviour // THAY ĐỔI
    {
        // --- TẤT CẢ CÁC BIẾN GIỮ NGUYÊN ---
        [Header("Jump Settings")]
        public float m_JumpForce = 5f;  // Lực nhảy
        public LayerMask m_GroundMask; // Lớp mặt đất để kiểm tra tiếp đất
        public Transform m_GroundCheck; // Điểm kiểm tra tiếp đất
        public float m_GroundCheckRadius = 0.3f; // Bán kính kiểm tra tiếp đất

        private InputAction m_JumpAction; // Hành động nhảy
        private bool m_IsGrounded = false; // Trạng thái tiếp đất

        [Tooltip("Số người chơi. Không có menu chọn obj, Người chơi 1 điều khiển bằng bàn phím trái, Người chơi 2 điều khiển bằng bàn phím phải")]
        public int m_PlayerNumber = 1;                   // Dùng để xác định obj nào thuộc về người chơi nào. Cái này được thiết lập bởi bộ quản lý của obj.
        [Tooltip("Tốc độ (đơn vị unity/giây) mà obj di chuyển")]
        public float m_Speed = 12f;                      // Tốc độ obj di chuyển tới và lùi.
        [Tooltip("Tốc độ xoay của obj theo độ/giây")]
        public float m_TurnSpeed = 180f;                 // Tốc độ obj quay theo độ mỗi giây.
        [Tooltip("Nếu đặt thành true, obj tự động định hướng và di chuyển theo hướng nhấn thay vì xoay trái/phải và di chuyển tiến lên")]
        public bool m_IsDirectControl;
        public AudioSource m_MovementAudio;              // Tham chiếu đến nguồn âm thanh dùng để phát tiếng động cơ. NB: khác với nguồn âm thanh bắn.
        public AudioClip m_EngineIdling;                 // Âm thanh phát khi obj không di chuyển.
        public AudioClip m_EngineDriving;                // Âm thanh phát khi obj đang di chuyển.
        public float m_PitchRange = 0.2f;                // Phạm vi thay đổi cao độ của tiếng động cơ.
        [Tooltip("Nếu đặt thành true, obj này sẽ được điều khiển bởi máy tính chứ không phải người chơi")]
        public bool m_IsComputerControlled = false; // obj này do người chơi hay máy tính điều khiển
        [HideInInspector]
        public TankInputUser m_InputUser;                // Thành phần Input User cho obj đó. Chứa các Input Action.
        public Rigidbody Rigidbody => m_Rigidbody;
        public int ControlIndex { get; set; } = -1; // Cái này định nghĩa chỉ số điều khiển 1 = bàn phím trái hoặc gamepad, 2 = bàn phím phải, -1 = không điều khiển

        private string m_MovementAxisName;           // Tên trục input để di chuyển tiến và lùi.
        private string m_TurnAxisName;               // Tên trục input để quay.
        private Rigidbody m_Rigidbody;               // Tham chiếu dùng để di chuyển obj.
        private float m_MovementInputValue;          // Giá trị hiện tại của input di chuyển.
        private float m_TurnInputValue;              // Giá trị hiện tại của input quay.
        private float m_OriginalPitch;               // Cao độ gốc của nguồn âm thanh khi bắt đầu cảnh.
        private ParticleSystem[] m_particleSystems; // Tham chiếu đến tất cả các hệ thống hạt được sử dụng bởi obj

        private InputAction m_MoveAction;            // InputAction dùng để di chuyển, được lấy từ TankInputUser
        private InputAction m_TurnAction;            // InputAction dùng để bắn, được lấy từ TankInputUser

        private Vector3 m_RequestedDirection;        // Trong chế độ điều khiển trực tiếp, lưu trữ hướng người dùng *muốn* đi tới
        private Animator m_Animator;
        private bool m_WasJumping = false;


        // --- CÁC HÀM KHỞI TẠO (Awake, OnEnable, OnDisable, Start) GIỮ NGUYÊN ---
        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();

            m_InputUser = GetComponent<TankInputUser>();
            if (m_InputUser == null)
                m_InputUser = gameObject.AddComponent<TankInputUser>();
            m_Animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            m_Rigidbody.isKinematic = false;
            m_MovementInputValue = 0f;
            m_TurnInputValue = 0f;
            m_particleSystems = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < m_particleSystems.Length; ++i)
            {
                m_particleSystems[i].Play();
            }
        }

        private void OnDisable()
        {
            m_Rigidbody.isKinematic = true;
            for (int i = 0; i < m_particleSystems.Length; ++i)
            {
                m_particleSystems[i].Stop();
            }
        }

        // OnNetworkSpawn được gọi thay cho Start khi làm việc với Netcode
        public override void OnNetworkSpawn()
        {
            if (m_IsComputerControlled)
            {
                // nhưng nó không có thành phần AI...
                var ai = GetComponent<TankAI>();
                if (ai == null)
                {
                    // chúng ta thêm nó, để đảm bảo cái này sẽ điều khiển obj.
                    // Điều này chỉ hữu ích khi người dùng kiểm tra obj trong cảnh trống, nếu không TankManager đảm bảo
                    // obj do máy tính điều khiển được thiết lập đúng cách
                    gameObject.AddComponent<TankAI>();
                }
            }

            // Chỉ chủ sở hữu mới cần thiết lập input
            if (IsOwner)
            {
                if (ControlIndex == -1 && !m_IsComputerControlled)
                {
                    ControlIndex = m_PlayerNumber;
                }

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

                m_MovementAxisName = "Vertical";
                m_TurnAxisName = "Horizontal";

                m_MoveAction = m_InputUser.ActionAsset.FindAction(m_MovementAxisName);
                m_TurnAction = m_InputUser.ActionAsset.FindAction(m_TurnAxisName);
                m_JumpAction = m_InputUser.ActionAsset.FindAction("Jump");

                if (m_JumpAction != null) m_JumpAction.Enable();
                m_MoveAction.Enable();
                m_TurnAction.Enable();
            }

            m_OriginalPitch = m_MovementAudio.pitch;
        }

        private void Update()
        {
            // THAY ĐỔI LOGIC: Chỉ chủ sở hữu mới đọc input
            if (IsOwner && !m_IsComputerControlled)
            {
                m_MovementInputValue = m_MoveAction.ReadValue<float>();
                m_TurnInputValue = m_TurnAction.ReadValue<float>();
                HandleJump(); // Chỉ chủ sở hữu mới có thể nhảy
            }

            // Các hàm hiển thị chạy cho tất cả mọi người
            CheckGrounded();
            EngineAudio();
            UpdateAnimatorStates();
        }

        private void CheckGrounded()
        {
            if (m_GroundCheck != null)
            {
                m_IsGrounded = Physics.CheckSphere(m_GroundCheck.position, m_GroundCheckRadius, m_GroundMask);
            }
        }

        private void HandleJump()
        {
            if (m_JumpAction != null && m_JumpAction.WasPressedThisFrame() && m_IsGrounded)
            {
                m_Rigidbody.AddForce(Vector3.up * m_JumpForce, ForceMode.Impulse);
            }
        }

        private void EngineAudio()
        {
            // THAY ĐỔI LOGIC: Dựa vào vận tốc thực tế, không phải input
            // Điều này đảm bảo client khác cũng nghe được âm thanh động cơ
            if (m_Rigidbody.linearVelocity.magnitude < 0.1f)
            {
                if (m_MovementAudio.clip == m_EngineDriving)
                {
                    m_MovementAudio.clip = m_EngineIdling;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
            else
            {
                if (m_MovementAudio.clip == m_EngineIdling)
                {
                    m_MovementAudio.clip = m_EngineDriving;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
        }

        private void UpdateAnimatorStates()
        {
            if (m_Animator == null) return;

            // THAY ĐỔI LOGIC: Dựa vào vận tốc thực tế, không phải input
            // Điều này đảm bảo client khác cũng thấy được hoạt ảnh di chuyển
            bool isMoving = m_Rigidbody.linearVelocity.magnitude > 0.1f;
            m_Animator.SetBool("isMoving", isMoving);

            bool isJumpingNow = !m_IsGrounded;
            if (isJumpingNow != m_WasJumping)
            {
                m_Animator.SetBool("isJumping", isJumpingNow);
                m_WasJumping = isJumpingNow;
            }
        }

        private void FixedUpdate()
        {
            // THAY ĐỔI: Chỉ chủ sở hữu mới thực hiện logic di chuyển
            if (!IsOwner) return;

            if (m_InputUser.InputUser.controlScheme.Value.name == "Gamepad" || m_IsDirectControl)
            {
                var camForward = Camera.main.transform.forward;
                camForward.y = 0;
                camForward.Normalize();
                var camRight = Vector3.Cross(Vector3.up, camForward);
                m_RequestedDirection = (camForward * m_MovementInputValue + camRight * m_TurnInputValue);
            }

            Move();
            Turn();
        }

        // --- CÁC HÀM Move() và Turn() GIỮ NGUYÊN ---
        private void Move()
        {
            float speedInput = 0.0f;
            if (m_InputUser.InputUser.controlScheme.Value.name == "Gamepad" || m_IsDirectControl)
            {
                speedInput = m_RequestedDirection.magnitude;
                speedInput *= 1.0f - Mathf.Clamp01((Vector3.Angle(m_RequestedDirection, transform.forward) - 90) / 90.0f);
            }
            else
            {
                speedInput = m_MovementInputValue;
            }
            Vector3 movement = transform.forward * speedInput * m_Speed * Time.deltaTime;
            m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
        }

        private void Turn()
        {
            Quaternion turnRotation;
            if (m_InputUser.InputUser.controlScheme.Value.name == "Gamepad" || m_IsDirectControl)
            {
                float angleTowardTarget = Vector3.SignedAngle(m_RequestedDirection, transform.forward, transform.up);
                var rotatingAngle = Mathf.Sign(angleTowardTarget) * Mathf.Min(Mathf.Abs(angleTowardTarget), m_TurnSpeed * Time.deltaTime);
                turnRotation = Quaternion.AngleAxis(-rotatingAngle, Vector3.up);
            }
            else
            {
                float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;
                turnRotation = Quaternion.Euler(0f, turn, 0f);
            }
            m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
        }
    }
}