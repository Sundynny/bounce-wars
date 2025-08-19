using UnityEngine;

public class AltarController : MonoBehaviour
{
    [SerializeField] private int m_TeamID = 1;

    private void OnTriggerEnter(Collider other)
    {
        TankFlagCarrier tank = other.GetComponent<TankFlagCarrier>();

        // Đổi tên thuộc tính kiểm tra
        if (tank != null && tank.IsCarryingAnyPointObject)
        {
            Debug.Log("Tank with " + tank.gameObject.GetComponent<TankFlagCarrier>().GetCarriedCount() + " points has reached the altar!");

            // GameManager.Instance.AddScore(m_TeamID, tank.GetCarriedCount()); // Cộng số điểm bằng số quả cầu

            tank.OnScore();
        }
    }
}