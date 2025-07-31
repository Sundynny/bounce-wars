using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Tanks.Complete
{
    // Đảm bảo script này chạy trước thành phần TankShooting vì TankShooting lấy InputUser từ đây khi không có
    // GameManager được thiết lập (được sử dụng trong quá trình học để kiểm tra obj trong các cảnh trống)
    [DefaultExecutionOrder(-10)]
    public class TankMovement : MonoBehaviour
    {
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
            // obj do máy tính điều khiển thì có tính chất kinematic
            m_Rigidbody.isKinematic = false;

            // Cũng đặt lại các giá trị input.
            m_MovementInputValue = 0f;
            m_TurnInputValue = 0f;

            // Chúng ta lấy tất cả các hệ thống hạt con của obj này để có thể Dừng/Phát chúng khi Deactivate/Activate
            // Điều này cần thiết vì chúng ta di chuyển obj khi spawn, và nếu hệ thống hạt đang phát khi chúng ta làm vậy
            // nó "nghĩ" nó di chuyển từ (0,0,0) đến điểm spawn, tạo ra một vệt khói lớn
            m_particleSystems = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < m_particleSystems.Length; ++i)
            {
                m_particleSystems[i].Play();
            }
        }


        private void OnDisable()
        {
            // Khi obj bị tắt, đặt nó thành kinematic để nó ngừng di chuyển.
            m_Rigidbody.isKinematic = true;

            // Dừng tất cả các hệ thống hạt để nó "đặt lại" vị trí của nó về vị trí thực tế thay vì nghĩ rằng chúng ta đã di chuyển khi spawn
            for (int i = 0; i < m_particleSystems.Length; ++i)
            {
                m_particleSystems[i].Stop();
            }
        }


        private void Start()
        {
            // Nếu đây là obj do máy tính điều khiển...
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

            // Nếu không có chỉ số điều khiển nào được đặt, điều này có nghĩa đây là một cảnh không có GameManager và obj đó đã được thêm thủ công
            // vào một cảnh trống, vì vậy chúng ta sử dụng Số người chơi được đặt thủ công trong Inspector làm ControlIndex,
            // để Người chơi 1 sẽ là ControlIndex 1 -> Bàn phím trái và Người chơi 2 -> Bàn phím phải
            if (ControlIndex == -1 && !m_IsComputerControlled)
            {
                ControlIndex = m_PlayerNumber;
            }

            var mobileControl = FindAnyObjectByType<MobileUIControl>();

            // Mặc định, ControlIndex 1 được khớp với KeyboardLeft. Nhưng nếu có một thành phần điều khiển UI di động trong cảnh
            // và nó đang hoạt động (vì vậy chúng ta đang ở trên thiết bị di động hoặc nó đã được kích hoạt bắt buộc để người dùng kiểm tra) thì chúng ta thay vào đó
            // khớp ControlIndex 1 với Gamepad ảo trên màn hình.
            if (mobileControl != null && ControlIndex == 1)
            {
                m_InputUser.SetNewInputUser(InputUser.PerformPairingWithDevice(mobileControl.Device));
                m_InputUser.ActivateScheme("Gamepad");
            }
            else
            {
                // ngược lại nếu không có điều khiển UI di động nào hoạt động, ControlIndex là scheme KeyboardLeft và ControlIndex 2 là KeyboardRight
                m_InputUser.ActivateScheme(ControlIndex == 1 ? "KeyboardLeft" : "KeyboardRight");
            }

            // Tên các trục dựa trên số người chơi.
            m_MovementAxisName = "Vertical";
            m_TurnAxisName = "Horizontal";

            // Lấy input hành động từ thành phần TankInputUser sẽ đảm nhiệm việc sao chép chúng và
            // liên kết chúng với thiết bị và sơ đồ điều khiển phù hợp
            m_MoveAction = m_InputUser.ActionAsset.FindAction(m_MovementAxisName);
            m_TurnAction = m_InputUser.ActionAsset.FindAction(m_TurnAxisName);
            m_JumpAction = m_InputUser.ActionAsset.FindAction("Jump");
            if (m_JumpAction != null)
            {
                m_JumpAction.Enable();
            }

            // Các hành động cần được kích hoạt trước khi chúng có thể phản ứng với input
            m_MoveAction.Enable();
            m_TurnAction.Enable();

            // Lưu cao độ gốc của nguồn âm thanh.
            m_OriginalPitch = m_MovementAudio.pitch;
        }


        private void Update()
        {
            // obj do máy tính điều khiển sẽ được di chuyển bởi thành phần TankAI, vì vậy chỉ đọc input cho obj do người chơi điều khiển
            if (!m_IsComputerControlled)
            {
                m_MovementInputValue = m_MoveAction.ReadValue<float>();
                m_TurnInputValue = m_TurnAction.ReadValue<float>();
            }

            EngineAudio();
            CheckGrounded();
            HandleJump();
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
            // Nếu không có input (obj đứng yên)...
            if (Mathf.Abs(m_MovementInputValue) < 0.1f && Mathf.Abs(m_TurnInputValue) < 0.1f)
            {
                // ... và nếu nguồn âm thanh hiện đang phát clip lái xe...
                if (m_MovementAudio.clip == m_EngineDriving)
                {
                    // ... thay đổi clip thành idling và phát nó.
                    m_MovementAudio.clip = m_EngineIdling;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
            else
            {
                // Ngược lại nếu obj đang di chuyển và nếu clip idling hiện đang phát...
                if (m_MovementAudio.clip == m_EngineIdling)
                {
                    // ... thay đổi clip thành lái xe và phát.
                    m_MovementAudio.clip = m_EngineDriving;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
        }

        private void UpdateAnimatorStates()
        {
            if (m_Animator == null)
                return;

            // Di chuyển: true nếu có input chuyển động
            bool isMoving = Mathf.Abs(m_MovementInputValue) > 0.1f || Mathf.Abs(m_TurnInputValue) > 0.1f;
            m_Animator.SetBool("isMoving", isMoving);

            // Nhảy: true khi vừa nhảy
            bool isJumpingNow = !m_IsGrounded;
            if (isJumpingNow != m_WasJumping)
            {
                m_Animator.SetBool("isJumping", isJumpingNow);
                m_WasJumping = isJumpingNow;
            }
        }

        private void FixedUpdate()
        {
            // Nếu cái này đang sử dụng gamepad hoặc đã bật điều khiển trực tiếp, cái này sử dụng một phương pháp di chuyển khác: thay vì
            // "lên" phía sau là di chuyển về phía trước cho obj, nó thay vào đó lấy hướng di chuyển của gamepad làm hướng mong muốn cho obj
            // và sẽ tính toán tốc độ và vòng quay cần thiết để di chuyển obj theo hướng đó.
            if (m_InputUser.InputUser.controlScheme.Value.name == "Gamepad" || m_IsDirectControl)
            {
                var camForward = Camera.main.transform.forward;
                camForward.y = 0;
                camForward.Normalize();
                var camRight = Vector3.Cross(Vector3.up, camForward);

                // cái này tạo ra một vector dựa trên hướng nhìn của camera (ví dụ: nhấn lên có nghĩa là chúng ta muốn đi lên theo hướng của
                // camera, không phải về phía trước theo hướng của obj)
                m_RequestedDirection = (camForward * m_MovementInputValue + camRight * m_TurnInputValue);
            }

            // Điều chỉnh vị trí và hướng của rigidbody trong FixedUpdate.
            Move();
            Turn();
        }


        private void Move()
        {
            float speedInput = 0.0f;

            // Trong chế độ điều khiển trực tiếp, tốc độ sẽ phụ thuộc vào khoảng cách chúng ta cách hướng mong muốn
            if (m_InputUser.InputUser.controlScheme.Value.name == "Gamepad" || m_IsDirectControl)
            {
                speedInput = m_RequestedDirection.magnitude;
                // nếu chúng ta điều khiển trực tiếp, tốc độ di chuyển dựa trên góc giữa hướng hiện tại và hướng mong muốn
                // hướng. Nếu dưới 90, tốc độ tối đa, sau đó tốc độ giảm giữa 90 và 180
                speedInput *= 1.0f - Mathf.Clamp01((Vector3.Angle(m_RequestedDirection, transform.forward) - 90) / 90.0f);
            }
            else
            {
                // trong chế độ điều khiển "obj" bình thường, giá trị tốc độ là mức độ chúng ta nhấn "lên/phía trước"
                speedInput = m_MovementInputValue;
            }

            // Tạo một vector theo hướng obj đang đối mặt với độ lớn dựa trên input, tốc độ và thời gian giữa các khung hình.
            Vector3 movement = transform.forward * speedInput * m_Speed * Time.deltaTime;

            // Áp dụng chuyển động này vào vị trí của rigidbody.
            m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
        }


        private void Turn()
        {
            Quaternion turnRotation;
            // Nếu trong chế độ điều khiển trực tiếp...
            if (m_InputUser.InputUser.controlScheme.Value.name == "Gamepad" || m_IsDirectControl)
            {
                // Tính toán vòng quay cần thiết để đạt được hướng mong muốn
                float angleTowardTarget = Vector3.SignedAngle(m_RequestedDirection, transform.forward, transform.up);
                var rotatingAngle = Mathf.Sign(angleTowardTarget) * Mathf.Min(Mathf.Abs(angleTowardTarget), m_TurnSpeed * Time.deltaTime);
                turnRotation = Quaternion.AngleAxis(-rotatingAngle, Vector3.up);
            }
            else
            {
                float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;

                // Biến cái này thành một vòng quay theo trục y.
                turnRotation = Quaternion.Euler(0f, turn, 0f);
            }

            // Áp dụng vòng quay này vào vòng quay của rigidbody.
            m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
        }
    }
}