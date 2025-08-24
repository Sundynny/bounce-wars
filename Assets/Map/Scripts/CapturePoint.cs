// CapturePoint.cs
using UnityEngine;
using UnityEngine.UI;
using System;

public class CapturePoint : MonoBehaviour
{
    [Header("Capture Settings")]
    [Tooltip("Thời gian (giây) để từ 0 -> 1 tiến trình (chiếm hoàn toàn) khi không bị tranh chấp")]
    public float captureTime = 5f;

    [Tooltip("Tên cờ duy nhất (A/B/C...) dùng cho quản lý điểm)")]
    public string pointId = "A";

    [Header("UI")]
    public Canvas uiCanvas;          // Canvas con đặt trong cờ
    public Image progressFillImage;  // Image fillAmount (0..1)
    public Image ownerTint;          // (tuỳ chọn) Image để đổi màu theo đội sở hữu

    public Team Owner { get; private set; } = Team.None;

    // runtime
    private int team1Inside = 0;
    private int team2Inside = 0;

    private Team capturingTeam = Team.None;    // đội hiện đang có quyền chiếm (không tranh chấp)
    private float progress = 0f;               // 0..1 tiến trình về phía capturingTeam HOẶC về phía Owner (chi tiết dưới)

    public event Action<string, Team> OnCaptured; // (pointId, newOwner)

    private void Awake()
    {
        SetUIVisible(false);
        UpdateOwnerVisual();
    }

    private void Update()
    {
        bool contested = team1Inside > 0 && team2Inside > 0;
        bool someoneInside = (team1Inside + team2Inside) > 0;

        // Xác định đội đang chiếm (nếu không tranh chấp)
        if (!contested)
        {
            if (team1Inside > 0) capturingTeam = Team.Team1;
            else if (team2Inside > 0) capturingTeam = Team.Team2;
            else capturingTeam = Team.None;
        }

        // Hiển thị UI khi có người chiếm VÀ không tranh chấp
        SetUIVisible(someoneInside && !contested && capturingTeam != Team.None);

        if (capturingTeam == Team.None || contested)
        {
            // Không ai chiếm hoặc tranh chấp -> dừng cập nhật tiến trình (có thể thêm decay nếu muốn)
            return;
        }

        float speed = 1f / Mathf.Max(0.01f, captureTime); // đơn vị "per second"
        float delta = speed * Time.deltaTime;

        if (Owner == Team.None)
        {
            // Đang ở trạng thái trung lập: tăng dần tiến trình tới 1 để gán Owner = capturingTeam
            progress = Mathf.MoveTowards(progress, 1f, delta);
            progressFillImage.fillAmount = progress;

            if (Mathf.Approximately(progress, 1f))
            {
                Owner = capturingTeam;
                UpdateOwnerVisual();
                OnCaptured?.Invoke(pointId, Owner);
            }
        }
        else if (Owner == capturingTeam)
        {
            // Đồng đội đang đứng trong cờ của mình: đảm bảo đầy (ổn định). Có thể bỏ đoạn này.
            progress = Mathf.MoveTowards(progress, 1f, delta);
            progressFillImage.fillAmount = progress;
        }
        else
        {
            // Đội khác đang chiếm: giảm về 0 -> mất quyền, rồi sẽ tiếp tục tăng lên 1 (vì Owner == None ở nhánh trên)
            progress = Mathf.MoveTowards(progress, 0f, delta);
            progressFillImage.fillAmount = progress;

            if (Mathf.Approximately(progress, 0f))
            {
                Owner = Team.None; // trở về trung lập, vòng sau sẽ tăng lên 1 và gán Owner = capturingTeam
                UpdateOwnerVisual();
                // UI vẫn giữ nguyên hiển thị để tiếp tục pha tăng lên 1
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var tm = other.GetComponentInParent<TeamMember>();
        if (tm == null) return;

        if (tm.Team == Team.Team1) team1Inside++;
        else if (tm.Team == Team.Team2) team2Inside++;
    }

    private void OnTriggerExit(Collider other)
    {
        var tm = other.GetComponentInParent<TeamMember>();
        if (tm == null) return;

        if (tm.Team == Team.Team1) team1Inside = Mathf.Max(0, team1Inside - 1);
        else if (tm.Team == Team.Team2) team2Inside = Mathf.Max(0, team2Inside - 1);
    }

    private void SetUIVisible(bool visible)
    {
        if (uiCanvas != null) uiCanvas.enabled = visible;
    }

    private void UpdateOwnerVisual()
    {
        if (ownerTint == null) return;

        // Màu tuỳ ý; bạn có thể thay bằng gradient, sprite, v.v.
        Color team1 = new Color(0.2f, 0.6f, 1f);
        Color team2 = new Color(1f, 0.4f, 0.2f);
        Color none = new Color(0.8f, 0.8f, 0.8f, 0.5f);

        if (Owner == Team.Team1) ownerTint.color = team1;
        else if (Owner == Team.Team2) ownerTint.color = team2;
        else ownerTint.color = none;
    }

    public bool IsContested => team1Inside > 0 && team2Inside > 0;
}
