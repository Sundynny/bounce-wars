using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Quản lý trạng thái của trận đấu, bao gồm thời gian, điểm số và các giai đoạn của trận đấu.
/// Script này chỉ nên tồn tại một lần trong scene và logic của nó chủ yếu chạy trên Server.
/// </summary>
public class GameManager : NetworkBehaviour
{
    // --- Singleton Pattern ---
    // Giúp các script khác có thể dễ dàng truy cập GameManager duy nhất.
    public static GameManager Instance { get; private set; }

    // --- Cấu hình Trận đấu ---
    [Header("Match Settings")]
    [SerializeField] private float matchDuration = 180f; // Thời gian trận đấu (giây), ví dụ: 3 phút

    // --- Trạng thái Trận đấu (Được đồng bộ) ---
    // Dữ liệu quan trọng cần đồng bộ cho tất cả người chơi.
    // Chỉ Server có quyền ghi.
    private NetworkVariable<float> matchTimer = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> team1Score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> team2Score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Enum để định nghĩa các trạng thái của trận đấu.
    public enum MatchState { WaitingToStart, InProgress, Finished }
    private NetworkVariable<MatchState> currentMatchState = new NetworkVariable<MatchState>(MatchState.WaitingToStart, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Public properties để các script khác (như UI) có thể đọc.
    public float MatchTimer => matchTimer.Value;
    public int Team1Score => team1Score.Value;
    public int Team2Score => team2Score.Value;
    public MatchState CurrentMatchState => currentMatchState.Value;

    private void Awake()
    {
        // Thiết lập Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    /// <summary>
    /// Hàm này được gọi khi GameManager được tạo ra trên mạng.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        // Chỉ Server mới có quyền thiết lập trạng thái ban đầu của trận đấu.
        if (IsServer)
        {
            currentMatchState.Value = MatchState.WaitingToStart;
            // TODO: Thêm logic chờ đủ người chơi ở đây.
            // Tạm thời, chúng ta sẽ bắt đầu ngay.
            StartGame();
        }

        // TODO: Client đăng ký lắng nghe sự thay đổi của các NetworkVariable để cập nhật UI.
        // matchTimer.OnValueChanged += (prev, next) => { /* Cập nhật UI thời gian */ };
        // team1Score.OnValueChanged += (prev, next) => { /* Cập nhật UI điểm đội 1 */ };
    }

    /// <summary>
    /// Update chỉ được Server sử dụng để đếm ngược thời gian trận đấu.
    /// </summary>
    private void Update()
    {
        if (!IsServer) return; // Chỉ Server mới chạy logic này.

        if (currentMatchState.Value == MatchState.InProgress)
        {
            matchTimer.Value -= Time.deltaTime;
            if (matchTimer.Value <= 0f)
            {
                EndGame();
            }
        }
    }

    /// <summary>
    /// Bắt đầu trận đấu. Chỉ được gọi bởi Server.
    /// </summary>
    private void StartGame()
    {
        if (!IsServer) return;

        Debug.Log("Game Started!");
        matchTimer.Value = matchDuration;
        currentMatchState.Value = MatchState.InProgress;

        // TODO: Gửi ClientRpc để hiển thị đếm ngược "3, 2, 1, GO!" và kích hoạt điều khiển của người chơi.
    }

    /// <summary>
    /// Kết thúc trận đấu. Chỉ được gọi bởi Server.
    /// </summary>
    private void EndGame()
    {
        if (!IsServer) return;

        Debug.Log("Game Finished!");
        currentMatchState.Value = MatchState.Finished;

        // TODO: Gửi ClientRpc để hiển thị bảng điểm cuối cùng và vô hiệu hóa điều khiển.
    }

    /// <summary>
    /// Hàm public để các script khác (như AltarController/TankFlagCarrier) gọi để cộng điểm.
    /// Chỉ nên được gọi trên Server.
    /// </summary>
    public void AddScore(int teamId, int amount)
    {
        if (!IsServer) return; // Đảm bảo chỉ Server mới có thể thay đổi điểm số.

        if (currentMatchState.Value != MatchState.InProgress) return; // Không cộng điểm nếu trận đấu đã kết thúc.

        if (teamId == 1)
        {
            team1Score.Value += amount;
        }
        else if (teamId == 2)
        {
            team2Score.Value += amount;
        }

        Debug.Log($"Team {teamId} scored {amount} points! Total: {(teamId == 1 ? team1Score.Value : team2Score.Value)}");

        // TODO: Thêm điều kiện kết thúc trận đấu nếu đạt đủ điểm.
    }
}