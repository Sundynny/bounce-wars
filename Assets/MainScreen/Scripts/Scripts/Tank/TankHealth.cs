using UnityEngine;
using UnityEngine.UI;

namespace Tanks.Complete
{
    public class TankHealth : MonoBehaviour
    {
        public float m_StartingHealth = 100f;               // Lượng máu mà mỗi xe tăng bắt đầu có.
        public Slider m_Slider;                             // Thanh trượt để biểu thị lượng máu hiện tại của xe tăng.
        public Image m_FillImage;                           // Thành phần hình ảnh của thanh trượt.
        public Color m_FullHealthColor = Color.green;       // Màu của thanh máu khi đầy.
        public Color m_ZeroHealthColor = Color.red;         // Màu của thanh máu khi hết máu.
        public GameObject m_ExplosionPrefab;                // Một prefab sẽ được khởi tạo trong Awake, sau đó được sử dụng mỗi khi xe tăng bị phá hủy.
        [HideInInspector] public bool m_HasShield;          // Xe tăng đã nhặt được vật phẩm tăng sức mạnh khiên chưa?


        private AudioSource m_ExplosionAudio;               // Nguồn âm thanh để phát khi xe tăng phát nổ.
        private ParticleSystem m_ExplosionParticles;        // Hệ thống hạt sẽ phát khi xe tăng bị phá hủy.
        private float m_CurrentHealth;                      // Lượng máu hiện tại của xe tăng.
        private bool m_Dead;                                // Xe tăng đã bị giảm máu xuống dưới 0 chưa?
        private float m_ShieldValue;                        // Tỷ lệ phần trăm sát thương giảm đi khi xe tăng có khiên.
        private bool m_IsInvincible;                        // Xe tăng có đang bất tử vào lúc này không?

        private void Awake()
        {
            // Khởi tạo prefab vụ nổ và lấy tham chiếu đến hệ thống hạt trên đó.
            m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();

            // Lấy tham chiếu đến nguồn âm thanh trên prefab đã được khởi tạo.
            m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();

            // Vô hiệu hóa prefab để nó có thể được kích hoạt khi cần thiết.
            m_ExplosionParticles.gameObject.SetActive(false);

            // Đặt giá trị tối đa của thanh trượt thành lượng máu tối đa mà xe tăng có thể có.
            m_Slider.maxValue = m_StartingHealth;
        }

        private void OnDestroy()
        {
            if (m_ExplosionParticles != null)
                Destroy(m_ExplosionParticles.gameObject);
        }

        private void OnEnable()
        {
            // Khi xe tăng được kích hoạt, hãy đặt lại máu của xe tăng và trạng thái sống/chết.
            m_CurrentHealth = m_StartingHealth;
            m_Dead = false;
            m_HasShield = false;
            m_ShieldValue = 0;
            m_IsInvincible = false;

            // Cập nhật giá trị và màu sắc của thanh trượt máu.
            SetHealthUI();
        }


        public void TakeDamage(float amount)
        {
            // Kiểm tra xem xe tăng có bất tử không.
            if (!m_IsInvincible)
            {
                // Giảm máu hiện tại theo lượng sát thương nhận vào.
                m_CurrentHealth -= amount * (1 - m_ShieldValue);

                // Thay đổi các yếu tố giao diện người dùng một cách thích hợp.
                SetHealthUI();

                // Nếu máu hiện tại bằng hoặc dưới 0 và chưa được ghi nhận, hãy gọi hàm OnDeath.
                if (m_CurrentHealth <= 0f && !m_Dead)
                {
                    OnDeath();
                }
            }
        }


        public void IncreaseHealth(float amount)
        {
            // Kiểm tra xem việc thêm lượng máu có giữ máu trong giới hạn tối đa không.
            if (m_CurrentHealth + amount <= m_StartingHealth)
            {
                // Nếu giá trị máu mới nằm trong giới hạn, hãy cộng thêm lượng đó.
                m_CurrentHealth += amount;
            }
            else
            {
                // Nếu lượng máu mới vượt quá máu khởi đầu, hãy đặt nó ở mức tối đa.
                m_CurrentHealth = m_StartingHealth;
            }

            // Thay đổi các yếu tố giao diện người dùng một cách thích hợp.
            SetHealthUI();
        }


        public void ToggleShield(float shieldAmount)
        {
            // Đảo ngược giá trị của biến có khiên.
            m_HasShield = !m_HasShield;

            // Thiết lập lượng sát thương sẽ được giảm bởi khiên.
            if (m_HasShield)
            {
                m_ShieldValue = shieldAmount;
            }
            else
            {
                m_ShieldValue = 0;
            }
        }

        public void ToggleInvincibility()
        {
            m_IsInvincible = !m_IsInvincible;
        }


        private void SetHealthUI()
        {
            // Đặt giá trị của thanh trượt một cách thích hợp.
            m_Slider.value = m_CurrentHealth;

            // Nội suy màu của thanh máu giữa các màu đã chọn dựa trên tỷ lệ phần trăm hiện tại của máu khởi đầu.
            m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
        }


        private void OnDeath()
        {
            // Đặt cờ để hàm này chỉ được gọi một lần.
            m_Dead = true;

            // Di chuyển prefab vụ nổ đã được khởi tạo đến vị trí của xe tăng và bật nó lên.
            m_ExplosionParticles.transform.position = transform.position;
            m_ExplosionParticles.gameObject.SetActive(true);

            // Phát hệ thống hạt của vụ nổ xe tăng.
            m_ExplosionParticles.Play();

            // Phát hiệu ứng âm thanh nổ của xe tăng.
            m_ExplosionAudio.Play();

            // Tắt đối tượng xe tăng.
            gameObject.SetActive(false);
        }
    }
}