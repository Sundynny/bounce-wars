using Tanks.Complete;
using UnityEngine;

public class ElementalOrb : MonoBehaviour
{
    public enum ElementType
    {
        Fire,   // Lửa: Tăng sát thương
        Water,  // Nước: Hồi máu
        Wind,   // Gió: Tăng tốc độ
        Earth   // Đất: Tăng phòng thủ (khiên)
    }

    public ElementType elementType;
    public float effectDuration = 5f;
    public float effectStrength = 10f;
    public float damageMultiplier = 1.5f;

    private void OnTriggerEnter(Collider other)
    {
        // Lấy thành phần PowerUpDetector từ đối tượng va chạm
        PowerUpDetector detector = other.GetComponent<PowerUpDetector>();

        // --- THAY ĐỔI QUAN TRỌNG ---
        // Chỉ cần kiểm tra xem đối tượng có phải là xe tăng không (có PowerUpDetector).
        // Không cần kiểm tra xem nó có đang dùng vật phẩm khác không nữa.
        if (detector != null)
        {
            // Áp dụng hiệu ứng
            ApplyEffect(detector);

            // Vô hiệu hóa và hủy quả cầu
            GetComponent<Renderer>().enabled = false;
            GetComponent<Collider>().enabled = false;
            Destroy(gameObject, 0.5f);
        }
    }

    private void ApplyEffect(PowerUpDetector detector)
    {
        switch (elementType)
        {
            case ElementType.Fire:
                detector.PowerUpSpecialShell(damageMultiplier);
                break;

            case ElementType.Water:
                detector.PowerUpHealing(effectStrength);
                break;

            case ElementType.Wind:
                detector.PowerUpSpeed(effectStrength, effectStrength, effectDuration);
                break;

            case ElementType.Earth:
                float shieldAmount = Mathf.Clamp(effectStrength, 0.1f, 0.9f);
                detector.PickUpShield(shieldAmount, effectDuration);
                break;
        }
    }
}