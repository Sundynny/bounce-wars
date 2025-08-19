using UnityEngine;

/// <summary>
/// Một script "đánh dấu" đơn giản để xác định một đối tượng là Altar (bàn thờ/khu vực ghi điểm).
/// Nó không chứa logic thực thi, chỉ chứa dữ liệu như TeamID.
/// </summary>
public class AltarController_NETS
    : MonoBehaviour
{
    // Bạn vẫn có thể giữ lại TeamID để phân biệt các khu vực ghi điểm khác nhau.
    [SerializeField] private int m_TeamID = 1;
    public int TeamID => m_TeamID;
}