using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


namespace Tanks.Complete
{
    /// <summary>
    /// Xử lý việc điều khiển xe tăng khi xe tăng được thiết lập do Máy tính điều khiển
    /// </summary>
    public class TankAI : MonoBehaviour
    {
        // Trạng thái có thể có của xe tăng do Máy tính điều khiển: hoặc là tìm kiếm mục tiêu hoặc là bỏ chạy khỏi nó
        enum State
        {
            Seek, // Tìm kiếm
            Flee  // Bỏ chạy
        }

        private TankMovement m_Movement;                // Tham chiếu đến script di chuyển
        private TankShooting m_Shooting;                // Tham chiếu đến script bắn

        private float m_PathfindTime = 0.5f;            // Chỉ kích hoạt tìm đường sau khoảng thời gian này, để không làm giảm hiệu suất
        private float m_PathfindTimer = 0.0f;           // Thời gian cho đến lần gọi tìm đường tiếp theo

        private Transform m_CurrentTarget = null;       // Transform mà xe tăng đang theo dõi
        private float m_MaxShootingDistance = 0.0f;     // Lưu trữ khoảng cách bắn tối đa dựa trên cài đặt của TankShooting

        private float m_TimeBetweenShot = 2.0f;         // Xe tăng AI có thời gian hồi chiêu giữa các phát bắn để tránh bắn spam
        private float m_ShotCooldown = 0.0f;            // Thời gian còn lại cho đến phát bắn tiếp theo

        private Vector3 m_LastTargetPosition;           // Vị trí của mục tiêu ở khung hình trước
        private float m_TimeSinceLastTargetMove;        // Bộ đếm thời gian xem mục tiêu đã không di chuyển trong bao lâu. Được sử dụng để kích hoạt trạng thái bỏ chạy

        private NavMeshPath m_CurrentPath = null;       // Đường đi hiện tại mà xe tăng đang theo.
        private int m_CurrentCorner = 0;                // Góc nào của đường đi mà xe tăng hiện đang tiến tới
        private bool m_IsMoving = false;                // Xe tăng hiện có đang di chuyển hay không (xe tăng dừng lại để bắn)

        private GameObject[] m_AllTanks;                // Danh sách tất cả các xe tăng trong cảnh.

        private State m_CurrentState = State.Seek;      // Trạng thái AI hiện tại của Xe tăng.

        private void Awake()
        {
            // Awake vẫn được gọi trên component bị vô hiệu hóa. Để người dùng có thể thử vô hiệu hóa AI trên một xe tăng duy nhất
            // chúng tôi đảm bảo rằng component không bị vô hiệu hóa trước khi khởi tạo mọi thứ
            if (!isActiveAndEnabled)
                return;

            m_Movement = GetComponent<TankMovement>();
            m_Shooting = GetComponent<TankShooting>();

            // đảm bảo rằng cả hai script di chuyển và bắn đều được đặt ở chế độ "máy tính điều khiển"
            m_Movement.m_IsComputerControlled = true;
            m_Shooting.m_IsComputerControlled = true;

            // để tránh tất cả các xe tăng do máy tính điều khiển cùng lúc tìm đường (và gây gánh nặng cho CPU), xe tăng AI có một thời gian ngẫu nhiên
            // để tìm đường sẽ phân bổ chúng trên nhiều khung hình
            m_PathfindTime = Random.Range(0.3f, 0.6f);

            // Tính toán và lưu trữ khoảng cách tối đa mà một phát bắn từ xe tăng này có thể đạt tới. Điều này sẽ được sử dụng khi quyết định khi nào
            // bắt đầu nạp đạn và khi nào bắn
            m_MaxShootingDistance = Vector3.Distance(m_Shooting.GetProjectilePosition(1.0f), transform.position);

            // Chúng tôi sử dụng FindObjectByType để lấy tất cả các Xe tăng, để không phụ thuộc vào GameManager để người dùng có thể thử thêm AI vào một
            // cảnh trống nơi chưa có GameManager nào được thêm vào.
            m_AllTanks = FindObjectsByType<TankMovement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Select(e => e.gameObject).ToArray();
        }

        // Nếu có GameManager, nó sẽ gọi hàm này sau khi tạo một xe tăng do máy tính điều khiển. Điều này chỉ thay thế
        // danh sách xe tăng bằng danh sách từ GameManager
        public void Setup(GameManager manager)
        {
            // Nếu điều này đang sử dụng manager.m_SpawnPoints.ToArray(), nó sẽ nhận được một mảng TankManager, nhưng m_AllTanks là một mảng Transform.
            // Hàm Select sẽ gọi hàm được truyền dưới dạng tham số trên mỗi mục trong danh sách (ở đây là TankManager) và tạo một danh sách mới
            // chứa những gì mỗi mục trả về. Hàm mà chúng ta truyền ở đây, e => e.m_Instance, trả về Transform của xe tăng mà TankManager quản lý
            // vì vậy, thực tế manager.m_SpawnPoints.Select(e => e.m_Instance) sẽ cung cấp một danh sách tất cả các transform của xe tăng.
            m_AllTanks = manager.m_SpawnPoints.Select(e => e.m_Instance).ToArray();
        }

        public void TurnOff()
        {
            enabled = false;
        }

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

        void SeekUpdate()
        {
            // Để giảm tải cho CPU, các xe tăng không tìm đường đến mục tiêu của chúng mỗi khung hình. Thay vào đó, chúng
            // chờ một chút giữa mỗi lần tìm đường. Chúng sẽ đi về phía một vị trí "lỗi thời" ở giữa, nhưng vì thời gian tìm đường là
            // dưới 1 giây, điều này không đáng chú ý về mặt hình ảnh và hiệu quả hơn nhiều so với việc cố gắng tìm đường hơn 30 lần mỗi giây
            if (m_PathfindTimer > m_PathfindTime)
            {
                // đặt lại thời gian kể từ lần tìm đường cuối cùng
                m_PathfindTimer = 0;

                // Điều này sẽ lưu trữ mỗi đường đi đến từng xe tăng trong cảnh
                NavMeshPath[] paths = new NavMeshPath[m_AllTanks.Length];

                // Khởi tạo chiều dài đường đi ngắn nhất bằng giá trị lớn nhất mà một float có thể có, vì vậy bất kể chiều dài
                // của đường đi đầu tiên được tìm thấy là bao nhiêu, nó chắc chắn sẽ ngắn hơn giá trị ban đầu này
                float shortestPath = float.MaxValue;
                // đường đi nào trong mảng paths mà chúng ta sử dụng. Mặc định là không có, được biểu thị bằng -1 ở đây.
                int usedPath = -1;
                Transform target = null;

                // Tính toán một đường đi đến mọi xe tăng và kiểm tra cái gần nhất
                for (var i = 0; i < m_AllTanks.Length; i++)
                {
                    var tank = m_AllTanks[i].gameObject;

                    // chúng ta không muốn xe tăng cố gắng nhắm vào chính nó, vì vậy hãy bỏ qua chính nó
                    if (tank == gameObject)
                        continue;

                    // đây là một xe tăng đã bị phá hủy hoặc bị vô hiệu hóa, đây không phải là một mục tiêu hợp lệ
                    if (tank == null || !tank.activeInHierarchy)
                        continue;

                    paths[i] = new NavMeshPath();

                    // điều này trả về true nếu tìm thấy một đường đi
                    if (NavMesh.CalculatePath(transform.position, tank.transform.position, ~0, paths[i]))
                    {
                        // Tính toán độ dài của đường đi...
                        float length = GetPathLength(paths[i]);
                        // Và nếu đó là con đường ngắn nhất cho đến nay, đây là con đường chúng ta muốn theo đuổi
                        if (shortestPath > length)
                        {
                            // vì vậy đường đi này trở thành đường đi được sử dụng
                            usedPath = i;
                            // và độ dài của nó bây giờ là độ dài ngắn nhất cần vượt qua
                            shortestPath = length;
                            target = tank.transform;
                        }
                    }
                }

                // usedPath vẫn sẽ là -1 nếu xe tăng không thể tìm thấy đường đi đến bất kỳ xe tăng nào, nếu không chúng ta có một mục tiêu
                if (usedPath != -1)
                {
                    // chúng ta đã chuyển mục tiêu. Xe tăng cuối cùng mà chúng ta đang tìm kiếm đã ở xa hơn một xe tăng khác, chiếc xe tăng mới này
                    // trở thành mục tiêu mới của chúng ta, và chúng ta đặt lại vị trí cuối cùng vì đây là một mục tiêu mới
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
            // Việc tìm đường bây giờ đã hoàn tất hoặc không được kích hoạt trong khung hình này vì nó đã được thực hiện đủ gần đây
            // SeekUpdate bây giờ tìm kiếm và cố gắng bắn vào mục tiêu mà nó có

            // Xe tăng này có một mục tiêu...
            if (m_CurrentTarget != null)
            {
                // kiểm tra xem mục tiêu của chúng ta đã di chuyển bao xa kể từ lần cập nhật cuối cùng
                float targetMovement = Vector3.Distance(m_CurrentTarget.position, m_LastTargetPosition);

                // mục tiêu không (hoặc gần như không) di chuyển...
                if (targetMovement < 0.0001f)
                {
                    // vì vậy chúng ta tăng bộ đếm thời gian. Điều này được sử dụng sau này, nếu một mục tiêu chúng ta đang bắn không di chuyển trong 2 giây, chúng ta sẽ bỏ chạy
                    m_TimeSinceLastTargetMove += Time.deltaTime;
                }
                else
                {
                    // mục tiêu đã di chuyển kể từ lần cuối, vì vậy chúng ta đặt lại bộ đếm thời gian kể từ lần di chuyển cuối cùng về 0.
                    m_TimeSinceLastTargetMove = 0;
                }

                // vị trí hiện tại trở thành vị trí cuối cùng sẽ được sử dụng trong khung hình tiếp theo để kiểm tra xem mục tiêu có di chuyển không
                m_LastTargetPosition = m_CurrentTarget.position;

                // Lấy một vector từ xe tăng này đến mục tiêu của nó
                Vector3 toTarget = m_CurrentTarget.position - transform.position;
                // bằng cách đặt y thành 0, chúng ta đảm bảo rằng vector đến mục tiêu nằm trên mặt phẳng của mặt đất
                toTarget.y = 0;

                float targetDistance = toTarget.magnitude;
                // chuẩn hóa vector đến mục tiêu, đặt độ dài của nó thành 1, điều này hữu ích cho một số phép toán.
                toTarget.Normalize();

                // tích vô hướng giữa 2 vector đã chuẩn hóa là cosin của góc giữa các vector đó. Điều này hữu ích vì nó
                // cho phép kiểm tra xem các vector đó thẳng hàng đến mức nào: 1 -> cùng hướng, 0 -> góc 90 độ, -1, chỉ theo hướng ngược lại.
                // Khi chúng ta tính tích vô hướng giữa vector phía trước của chúng ta và vector hướng tới mục tiêu, điều này cho chúng ta biết chúng ta đang
                // đối mặt với mục tiêu của mình đến mức nào: nếu giá trị này gần bằng 1, chúng ta đang đối mặt thẳng với mục tiêu.
                float dotToTarget = Vector3.Dot(toTarget, transform.forward);

                // nếu chúng ta đang nạp đạn, hãy kiểm tra xem phát bắn hiện tại có thể đến được mục tiêu không
                if (m_Shooting.IsCharging)
                {
                    // lấy điểm ước tính của đạn với giá trị nạp đạn hiện tại
                    Vector3 currentShotTarget = m_Shooting.GetProjectilePosition(m_Shooting.CurrentChargeRatio);
                    // khoảng cách từ chúng ta đến điểm ước tính đó
                    float currentShotDistance = Vector3.Distance(currentShotTarget, transform.position);

                    // nếu chúng ta đang đối mặt với mục tiêu và phát bắn của chúng ta đã được nạp đủ để đến được mục tiêu, hãy bắn
                    // lưu ý: chúng tôi trừ 2 khỏi khoảng cách mục tiêu vì phát bắn của chúng tôi có sát thương lan, vì vậy chúng tôi có thể bắn
                    // sớm hơn
                    if (currentShotDistance >= targetDistance - 2 && dotToTarget > 0.99f)
                    {
                        m_IsMoving = false;
                        m_Shooting.StopCharging();

                        // chúng ta vừa bắn, vì vậy chúng ta đặt thời gian hồi chiêu thành thời gian giữa các phát bắn (giá trị này được giảm đi mỗi khung hình trong hàm update)
                        m_ShotCooldown = m_TimeBetweenShot;

                        // Chúng ta vừa bắn, và mục tiêu của chúng ta đã không di chuyển trong một thời gian. Điều đó có nghĩa là họ có thể cũng đang nhắm và bắn vào chúng ta
                        // chúng ta chuyển sang chế độ bỏ chạy thay vì ở đó như một mục tiêu tĩnh
                        if (m_TimeSinceLastTargetMove > 2.0f)
                        {
                            StartFleeing();
                        }
                    }
                }
                else
                {
                    // Chúng ta chưa nạp đạn, vì vậy hãy kiểm tra xem mục tiêu có gần hơn khoảng cách bắn tối đa của chúng ta không, điều đó có nghĩa là chúng ta có thể bắt đầu nạp đạn
                    // (một giải pháp "thông minh hơn" sẽ là tính toán xem chúng ta có thể nạp đạn sớm đến mức nào để đạt được khoảng cách tối đa khi đã nạp đầy) 
                    if (targetDistance < m_MaxShootingDistance)
                    {
                        // Điều này sử dụng navmesh để kiểm tra xem có bất kỳ chướng ngại vật nào giữa chúng ta và mục tiêu không. Nếu điều này trả về false
                        // có nghĩa là không có đường đi không bị cản trở, vì vậy *có* một chướng ngại vật, vì vậy chúng ta chưa nên bắt đầu bắn
                        if (!NavMesh.Raycast(transform.position, m_CurrentTarget.position, out var hit, ~0))
                        {
                            // chúng ta ngừng di chuyển vì chúng ta có thể tiếp cận mục tiêu bằng phát bắn của mình
                            m_IsMoving = false;

                            // nếu thời gian hồi chiêu của chúng ta không phải là 0 hoặc thấp hơn, chúng ta phải đợi cho đến khi nó kết thúc mới được bắn. Nếu nó
                            // dưới 0, chúng ta bắt đầu nạp đạn
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
            // Khi bỏ chạy, xe tăng sẽ đi về phía một điểm ngẫu nhiên cách xa mục tiêu của nó. Khi chúng ta đến các góc cuối cùng
            // (tức là điểm) của đường đi đó, chúng ta có thể quay lại chế độ tìm kiếm
            if (m_CurrentCorner >= m_CurrentPath.corners.Length)
                m_CurrentState = State.Seek;
        }

        private void StartFleeing()
        {
            // Để bỏ chạy, chúng ta cần chọn một điểm cách xa mục tiêu hiện tại của mình

            // Bắt đầu bằng cách lấy vector *hướng tới* mục tiêu của chúng ta...
            var toTarget = (m_CurrentTarget.position - transform.position).normalized;

            // sau đó xoay vector đó một góc ngẫu nhiên từ 90 đến 180 độ, điều này sẽ cho chúng ta một hướng ngẫu nhiên
            // theo hướng ngược lại
            toTarget = Quaternion.AngleAxis(Random.Range(90.0f, 180.0f) * Mathf.Sign(Random.Range(-1.0f, 1.0f)),
                Vector3.up) * toTarget;

            // sau đó chúng ta chọn một điểm theo hướng ngẫu nhiên đó ở khoảng cách ngẫu nhiên từ 5 đến 20 đơn vị
            toTarget *= Random.Range(5.0f, 20.0f);

            // Cuối cùng, chúng ta tính toán một đường đi đến điểm ngẫu nhiên đó, và nó trở thành đường đi hiện tại mới của chúng ta.
            if (NavMesh.CalculatePath(transform.position, transform.position + toTarget, NavMesh.AllAreas,
                    m_CurrentPath))
            {
                m_CurrentState = State.Flee;
                m_CurrentCorner = 1;

                m_IsMoving = true;
            }
        }

        // Trái ngược với Update (được gọi mỗi khung hình mới, do đó được gọi một số lần thay đổi mỗi giây tùy thuộc
        // vào việc trò chơi đang kết xuất nhanh hay chậm), FixedUpdate được gọi theo một khoảng thời gian nhất định được xác định trong Cài đặt Vật lý
        // của dự án. Đây là nơi tất cả mã vật lý nên được đặt.
        private void FixedUpdate()
        {
            // Nếu xe tăng hiện không có đường đi, hãy thoát sớm.
            if (m_CurrentPath == null || m_CurrentPath.corners.Length == 0)
                return;

            var rb = m_Movement.Rigidbody;

            // Điểm mà chúng ta sẽ hướng tới. Theo mặc định, là góc hiện tại trong đường đi của chúng ta
            Vector3 orientTarget = m_CurrentPath.corners[Mathf.Min(m_CurrentCorner, m_CurrentPath.corners.Length - 1)];

            // nếu chúng ta không di chuyển, chúng ta sẽ hướng về mục tiêu của mình
            if (!m_IsMoving)
                orientTarget = m_CurrentTarget.position;

            Vector3 toOrientTarget = orientTarget - transform.position;
            toOrientTarget.y = 0;
            toOrientTarget.Normalize();

            Vector3 forward = rb.rotation * Vector3.forward;

            float orientDot = Vector3.Dot(forward, toOrientTarget);
            float rotatingAngle = Vector3.SignedAngle(toOrientTarget, forward, Vector3.up);

            // nếu chúng ta đang di chuyển, chúng ta di chuyển theo hướng về phía trước với tốc độ tối đa
            float moveAmount = Mathf.Clamp01(orientDot) * m_Movement.m_Speed * Time.deltaTime;
            if (m_IsMoving && moveAmount > 0.000001f)
            {
                rb.MovePosition(rb.position + forward * moveAmount);
            }

            // vòng quay thực tế cho khung hình đó là giá trị nhỏ nhất giữa tốc độ quay tối đa cho khung thời gian đó và
            // chính góc đó. Nhân với dấu của góc để đảm bảo chúng ta xoay đúng hướng
            rotatingAngle = Mathf.Sign(rotatingAngle) * Mathf.Min(Mathf.Abs(rotatingAngle), m_Movement.m_TurnSpeed * Time.deltaTime);

            if (Mathf.Abs(rotatingAngle) > 0.000001f)
                rb.MoveRotation(rb.rotation * Quaternion.AngleAxis(-rotatingAngle, Vector3.up));

            // Nếu chúng ta đã đến được mục tiêu hiện tại, chúng ta sẽ tăng góc của mình. Chúng ta sẽ không bao giờ đến được mục tiêu khi mục tiêu
            // là một chiếc xe tăng khác vì chúng ta dừng lại trước đó.
            if (Vector3.Distance(rb.position, orientTarget) < 0.5f)
            {
                m_CurrentCorner += 1;
            }
        }

        // Hàm tiện ích sẽ cộng chiều dài của tất cả các đoạn của đường đi đã cho để có được chiều dài hiệu dụng của nó
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