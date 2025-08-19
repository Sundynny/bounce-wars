using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;
using Unity.Netcode; // --- THAY ĐỔI NETCODE ---

namespace Tanks.Complete
{
    /// <summary>
    /// Xử lý việc điều khiển xe tăng khi xe tăng được thiết lập do Máy tính điều khiển
    /// </summary>
    // --- THAY ĐỔI NETCODE ---
    public class TankAI_NETCODE : NetworkBehaviour
    {
        // --- CÁC BIẾN VÀ COMMENT GỐC CỦA BẠN ĐƯỢC GIỮ NGUYÊN ---
        enum State
        {
            Seek, // Tìm kiếm
            Flee  // Bỏ chạy
        }

        private TankMovement m_Movement;
        private TankShooting m_Shooting;
        private float m_PathfindTime = 0.5f;
        private float m_PathfindTimer = 0.0f;
        private Transform m_CurrentTarget = null;
        private float m_MaxShootingDistance = 0.0f;
        private float m_TimeBetweenShot = 2.0f;
        private float m_ShotCooldown = 0.0f;
        private Vector3 m_LastTargetPosition;
        private float m_TimeSinceLastTargetMove;
        private NavMeshPath m_CurrentPath = null;
        private int m_CurrentCorner = 0;
        private bool m_IsMoving = false;
        private GameObject[] m_AllTanks;
        private State m_CurrentState = State.Seek;

        private void Awake()
        {
            // Awake vẫn được gọi trên component bị vô hiệu hóa. Để người dùng có thể thử vô hiệu hóa AI trên một xe tăng duy nhất
            // chúng tôi đảm bảo rằng component không bị vô hiệu hóa trước khi khởi tạo mọi thứ
            if (!isActiveAndEnabled)
                return;

            m_Movement = GetComponent<TankMovement>();
            m_Shooting = GetComponent<TankShooting>();

            // --- THAY ĐỔI NETCODE ---
            // Phần này vẫn có thể chạy ở Awake để thiết lập các tham chiếu ban đầu.
            // Việc bật/tắt AI sẽ được quản lý trong OnNetworkSpawn.
            m_Movement.m_IsComputerControlled = true;
            m_Shooting.m_IsComputerControlled = true;
        }

        // --- THAY ĐỔI NETCODE ---
        // Sử dụng OnNetworkSpawn để khởi tạo logic mạng.
        public override void OnNetworkSpawn()
        {
            // Quan trọng nhất: AI chỉ nên chạy trên Server.
            // Các client không cần chạy script này, họ chỉ cần xem kết quả.
            if (!IsServer)
            {
                enabled = false; // Tắt hoàn toàn script này trên client.
                return;
            }

            // Các thiết lập ban đầu chỉ chạy trên Server
            m_PathfindTime = Random.Range(0.3f, 0.6f);
            m_MaxShootingDistance = Vector3.Distance(m_Shooting.GetProjectilePosition(1.0f), transform.position);

            // Lấy danh sách xe tăng. Lưu ý: cách này chỉ hoạt động nếu tất cả xe tăng đều có mặt khi AI này spawn.
            // Một cách tốt hơn là quản lý danh sách này thông qua một GameManager trên server.
            m_AllTanks = FindObjectsByType<TankMovement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Select(e => e.gameObject).ToArray();
        }

        // Hàm này không thay đổi, nó vẫn hữu ích nếu bạn có một GameManager trên server.
        public void Setup(GameManager manager)
        {
            if (!IsServer) return; // Chỉ server mới có thể setup
            m_AllTanks = manager.m_SpawnPoints.Select(e => e.m_Instance).ToArray();
        }

        public void TurnOff()
        {
            enabled = false;
        }

        // --- THAY ĐỔI NETCODE ---
        // Các hàm vòng lặp (Update, FixedUpdate) sẽ tự động chỉ chạy trên server vì chúng ta đã tắt script trên client.
        // Không cần thêm if(!IsServer) ở đây, nhưng thêm vào cũng không hại gì.
        void Update()
        {
            // Nếu có thời gian hồi chiêu đang hoạt động, chúng ta giảm nó đi bằng thời gian đã trôi qua kể từ khung hình trước
            if (m_ShotCooldown > 0)
                m_ShotCooldown -= Time.deltaTime;

            // tăng thời gian kể từ lần tìm đường cuối cùng. SeekUpdate sẽ kiểm tra xem nó có vượt quá thời gian tìm đường không
            // và liệu có cần kích hoạt một lần tìm đường mới hay không
            m_PathfindTimer += Time.deltaTime;

            switch (m_CurrentState)
            {
                case State.Seek:
                    SeekUpdate();
                    break;
                case State.Flee:
                    FleeUpdate();
                    break;
            }
        }

        // Tất cả các hàm logic bên trong (SeekUpdate, FleeUpdate, v.v.) không cần thay đổi
        // vì chúng chỉ được gọi từ Update(), vốn đã được đảm bảo chỉ chạy trên Server.
        void SeekUpdate()
        {
            // ... (TOÀN BỘ LOGIC BÊN TRONG GIỮ NGUYÊN) ...
            if (m_PathfindTimer > m_PathfindTime)
            {
                m_PathfindTimer = 0;
                NavMeshPath[] paths = new NavMeshPath[m_AllTanks.Length];
                float shortestPath = float.MaxValue;
                int usedPath = -1;
                Transform target = null;

                for (var i = 0; i < m_AllTanks.Length; i++)
                {
                    var tank = m_AllTanks[i].gameObject;
                    if (tank == gameObject) continue;
                    if (tank == null || !tank.activeInHierarchy) continue;

                    paths[i] = new NavMeshPath();

                    if (NavMesh.CalculatePath(transform.position, tank.transform.position, ~0, paths[i]))
                    {
                        float length = GetPathLength(paths[i]);
                        if (shortestPath > length)
                        {
                            usedPath = i;
                            shortestPath = length;
                            target = tank.transform;
                        }
                    }
                }

                if (usedPath != -1)
                {
                    if (target != m_CurrentTarget)
                    {
                        m_CurrentTarget = target;
                        m_LastTargetPosition = m_CurrentTarget.position;
                    }
                    m_CurrentTarget = target;
                    m_CurrentPath = paths[usedPath];
                    m_CurrentCorner = 1;
                    m_IsMoving = true;
                }
            }

            if (m_CurrentTarget != null)
            {
                float targetMovement = Vector3.Distance(m_CurrentTarget.position, m_LastTargetPosition);
                if (targetMovement < 0.0001f)
                {
                    m_TimeSinceLastTargetMove += Time.deltaTime;
                }
                else
                {
                    m_TimeSinceLastTargetMove = 0;
                }

                m_LastTargetPosition = m_CurrentTarget.position;
                Vector3 toTarget = m_CurrentTarget.position - transform.position;
                toTarget.y = 0;
                float targetDistance = toTarget.magnitude;
                toTarget.Normalize();
                float dotToTarget = Vector3.Dot(toTarget, transform.forward);

                if (m_Shooting.IsCharging)
                {
                    Vector3 currentShotTarget = m_Shooting.GetProjectilePosition(m_Shooting.CurrentChargeRatio);
                    float currentShotDistance = Vector3.Distance(currentShotTarget, transform.position);
                    if (currentShotDistance >= targetDistance - 2 && dotToTarget > 0.99f)
                    {
                        m_IsMoving = false;
                        // --- THAY ĐỔI NETCODE ---
                        // Hàm StopCharging của TankShooting đã được sửa để gọi ServerRpc, nên nó sẽ hoạt động đúng.
                        m_Shooting.StopCharging();
                        m_ShotCooldown = m_TimeBetweenShot;

                        if (m_TimeSinceLastTargetMove > 2.0f)
                        {
                            StartFleeing();
                        }
                    }
                }
                else
                {
                    if (targetDistance < m_MaxShootingDistance)
                    {
                        if (!NavMesh.Raycast(transform.position, m_CurrentTarget.position, out var hit, ~0))
                        {
                            m_IsMoving = false;
                            if (m_ShotCooldown <= 0.0f)
                            {
                                m_Shooting.StartCharging();
                            }
                        }
                    }
                }
            }
        }

        private void FleeUpdate()
        {
            if (m_CurrentPath != null && m_CurrentCorner >= m_CurrentPath.corners.Length)
                m_CurrentState = State.Seek;
        }

        private void StartFleeing()
        {
            var toTarget = (m_CurrentTarget.position - transform.position).normalized;
            toTarget = Quaternion.AngleAxis(Random.Range(90.0f, 180.0f) * Mathf.Sign(Random.Range(-1.0f, 1.0f)), Vector3.up) * toTarget;
            toTarget *= Random.Range(5.0f, 20.0f);

            NavMeshPath path = new NavMeshPath(); // Tạo một đối tượng path cục bộ
            if (NavMesh.CalculatePath(transform.position, transform.position + toTarget, NavMesh.AllAreas, path))
            {
                m_CurrentPath = path; // Chỉ gán nếu tìm thấy đường đi
                m_CurrentState = State.Flee;
                m_CurrentCorner = 1;
                m_IsMoving = true;
            }
        }

        private void FixedUpdate()
        {
            if (m_CurrentPath == null || m_CurrentPath.corners.Length == 0) return;

            var rb = m_Movement.Rigidbody;
            Vector3 orientTarget = m_CurrentPath.corners[Mathf.Min(m_CurrentCorner, m_CurrentPath.corners.Length - 1)];

            if (!m_IsMoving && m_CurrentTarget != null)
                orientTarget = m_CurrentTarget.position;

            Vector3 toOrientTarget = orientTarget - transform.position;
            toOrientTarget.y = 0;
            toOrientTarget.Normalize();
            Vector3 forward = rb.rotation * Vector3.forward;
            float orientDot = Vector3.Dot(forward, toOrientTarget);
            float rotatingAngle = Vector3.SignedAngle(toOrientTarget, forward, Vector3.up);

            float moveAmount = Mathf.Clamp01(orientDot) * m_Movement.m_Speed * Time.deltaTime;
            if (m_IsMoving && moveAmount > 0.000001f)
            {
                rb.MovePosition(rb.position + forward * moveAmount);
            }

            rotatingAngle = Mathf.Sign(rotatingAngle) * Mathf.Min(Mathf.Abs(rotatingAngle), m_Movement.m_TurnSpeed * Time.deltaTime);
            if (Mathf.Abs(rotatingAngle) > 0.000001f)
                rb.MoveRotation(rb.rotation * Quaternion.AngleAxis(-rotatingAngle, Vector3.up));

            if (Vector3.Distance(rb.position, orientTarget) < 0.5f)
            {
                m_CurrentCorner += 1;
            }
        }

        float GetPathLength(NavMeshPath path)
        {
            float dist = 0;
            for (var i = 1; i < path.corners.Length; ++i)
            {
                dist += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
            return dist;
        }
    }
}