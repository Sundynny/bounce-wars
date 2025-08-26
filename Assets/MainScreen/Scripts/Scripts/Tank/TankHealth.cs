using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Tanks.Complete
{
    public class TankHealth : MonoBehaviour
    {
        // --- CÀI ĐẶT UI GIÁP ẢO ---
        [Header("Shield UI Settings")]
        [Tooltip("Kéo Slider của thanh giáp vào đây. Nó sẽ hiển thị đè lên thanh máu.")]
        public Slider m_ShieldSlider;
        // Lưu ý: Chúng ta không cần GameObject cha nữa, chỉ cần Slider là đủ.

        // --- CÁC BIẾN CÀI ĐẶT CŨ ---
        [Header("Health & Respawn")]
        public float m_StartingHealth = 100f;
        public Slider m_Slider;
        public Image m_FillImage;
        public Color m_FullHealthColor = Color.green;
        public Color m_ZeroHealthColor = Color.red;
        public GameObject m_ExplosionPrefab;
        public float respawnTime = 5f;
        public GameObject respawnPanel;
        public Text respawnText;
        public Transform respawnPoint;

        [Header("Animation Settings")]
        public Animator m_Animator;

        // Các biến public không có trong Inspector
        [HideInInspector] public bool m_HasShield;

        // Các biến private
        private AudioSource m_ExplosionAudio;
        private ParticleSystem m_ExplosionParticles;
        private float m_CurrentHealth;
        private bool m_Dead;
        private bool m_IsInvincible;
        public Text logStatus;

        // --- BIẾN MỚI ĐỂ QUẢN LÝ GIÁP ---
        private float m_CurrentShield; // Lượng giáp hiện tại.
        private Coroutine m_ShieldCoroutine; // Tham chiếu đến coroutine của khiên để có thể làm mới.

        // Awake được gọi khi script được tải.
        private void Awake()
        {
            m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();
            m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();
            m_ExplosionParticles.gameObject.SetActive(false);

            if (m_Animator == null)
            {
                m_Animator = GetComponentInChildren<Animator>();
            }

            // Cài đặt giá trị tối đa cho thanh máu.
            if (m_Slider != null)
                m_Slider.maxValue = m_StartingHealth;
            // Cài đặt giá trị tối đa cho thanh giáp.
            if (m_ShieldSlider != null)
                m_ShieldSlider.maxValue = m_StartingHealth;
        }

        // OnDestroy được gọi khi GameObject bị phá hủy.
        private void OnDestroy()
        {
            if (m_ExplosionParticles != null)
                Destroy(m_ExplosionParticles.gameObject);
        }

        // OnEnable được gọi khi GameObject được kích hoạt (ví dụ: khi bắt đầu hoặc hồi sinh).
        private void OnEnable()
        {
            // Reset tất cả các trạng thái về ban đầu.
            m_CurrentHealth = m_StartingHealth;
            m_Dead = false;
            m_CurrentShield = 0; // Bắt đầu không có giáp.
            m_HasShield = false;
            m_IsInvincible = false;

            if (m_Animator != null)
            {
                m_Animator.SetBool("isDead", false);
            }

            // Cập nhật cả hai thanh UI.
            SetHealthUI();
            SetShieldUI();
        }

        // --- HÀM TakeDamage ĐÃ ĐƯỢC ĐẠI TU ĐỂ XỬ LÝ GIÁP ---
        public void TakeDamage(float amount, bool isDirectHit)
        {
            // Nếu đã chết hoặc đang bất tử, không nhận sát thương.
            if (m_Dead || m_IsInvincible) return;

            // --- LOGIC MỚI: Sát thương sẽ trừ vào giáp trước tiên ---
            float damageRemaining = amount;

            // Nếu nhân vật đang có giáp...
            if (m_CurrentShield > 0)
            {
                // Tính toán lượng sát thương thực tế sẽ trừ vào giáp (không thể trừ nhiều hơn lượng giáp đang có).
                float damageToShield = Mathf.Min(damageRemaining, m_CurrentShield);

                // Trừ giáp và giảm lượng sát thương còn lại.
                m_CurrentShield -= damageToShield;
                damageRemaining -= damageToShield;
            }

            // Nếu vẫn còn sát thương sau khi đã phá hết giáp...
            if (damageRemaining > 0)
            {
                // ...thì mới trừ vào máu.
                m_CurrentHealth -= damageRemaining;
            }

            // Sau khi tính toán xong, cập nhật lại cả hai thanh UI.
            SetHealthUI();
            SetShieldUI();

            // Kích hoạt animation bị thương nếu là đòn đánh trực tiếp và chưa chết.
            if (m_Animator != null && isDirectHit && m_CurrentHealth > 0f)
            {
                m_Animator.SetTrigger("Hurt");
            }

            // Kiểm tra xem nhân vật đã chết chưa.
            if (m_CurrentHealth <= 0f && !m_Dead)
            {
                OnDeath();
            }
        }

        // --- HÀM MỚI: Được gọi bởi PowerUpDetector để thêm giáp ---
        public void AddShield(float amount, float duration)
        {
            // Nếu đang có một coroutine khiên chạy, dừng nó lại để làm mới thời gian và lượng giáp.
            if (m_ShieldCoroutine != null)
            {
                StopCoroutine(m_ShieldCoroutine);
            }

            // Bắt đầu một coroutine mới để quản lý vòng đời của lớp giáp này.
            m_ShieldCoroutine = StartCoroutine(ShieldCoroutine(amount, duration));
        }

        // Coroutine quản lý thời gian tồn tại và giá trị của khiên.
        private IEnumerator ShieldCoroutine(float amount, float duration)
        {
            // Cộng thêm giáp, nhưng không cho phép vượt quá máu tối đa.
            m_CurrentShield = Mathf.Min(m_StartingHealth, m_CurrentShield + amount);
            m_HasShield = true; // Cập nhật cờ trạng thái.
            SetShieldUI(); // Cập nhật UI.

            // Chờ hết thời gian hiệu lực của khiên.
            yield return new WaitForSeconds(duration);

            // Hết giờ, xóa toàn bộ giáp.
            m_CurrentShield = 0;
            m_HasShield = false;
            SetShieldUI(); // Cập nhật lại UI (thanh giáp sẽ biến mất).
            m_ShieldCoroutine = null; // Đánh dấu là coroutine đã kết thúc.
        }

        // Hàm cập nhật UI cho thanh giáp.
        private void SetShieldUI()
        {
            if (m_ShieldSlider == null) return;

            // Cập nhật giá trị của thanh trượt giáp.
            // Nếu m_CurrentShield là 0, thanh giáp sẽ tự động trống rỗng.
            m_ShieldSlider.value = m_CurrentShield;
        }

        // --- CÁC HÀM CŨ KHÁC ---
        public void IncreaseHealth(float amount)
        {
            // Hồi máu, không cho vượt quá máu tối đa.
            m_CurrentHealth = Mathf.Min(m_CurrentHealth + amount, m_StartingHealth);
            SetHealthUI();
        }

        public void ToggleInvincibility() { m_IsInvincible = !m_IsInvincible; }

        private void SetHealthUI()
        {
            if (m_Slider == null) return;
            m_Slider.value = m_CurrentHealth;
            m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
        }

        public void OnDeath()
        {
            if (logStatus != null) logStatus.text = $"{gameObject.name} has been defeated";
            m_Dead = true;
            if (m_Animator != null) m_Animator.SetBool("isDead", true);

            m_ExplosionParticles.transform.position = transform.position;
            m_ExplosionParticles.gameObject.SetActive(true);
            m_ExplosionParticles.Play();
            m_ExplosionAudio.Play();

            // Bắt đầu quá trình hồi sinh.
            StartCoroutine(HandleRespawn());
        }

        public void PlayCelebrateAnimation()
        {
            if (m_Animator != null) m_Animator.SetTrigger("Celebrate");
        }

        private IEnumerator HandleRespawn()
        {
            // Vô hiệu hóa hình ảnh và va chạm.
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers) renderer.enabled = false;
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var collider in colliders) collider.enabled = false;

            // Hiển thị UI đếm ngược.
            if (respawnPanel != null) respawnPanel.SetActive(true);
            float timeLeft = respawnTime;
            while (timeLeft > 0)
            {
                if (respawnText != null) respawnText.text = Mathf.Ceil(timeLeft).ToString();
                yield return new WaitForSeconds(1f);
                timeLeft--;
            }
            if (respawnPanel != null) respawnPanel.SetActive(false);

            // Reset trạng thái sau khi hồi sinh.
            m_CurrentHealth = m_StartingHealth;
            m_Dead = false;
            transform.position = respawnPoint.position;

            // Reset lại giáp khi hồi sinh.
            m_CurrentShield = 0;
            m_HasShield = false;
            SetShieldUI();

            // Bật lại hình ảnh và va chạm.
            foreach (var renderer in renderers) renderer.enabled = true;
            foreach (var collider in colliders) collider.enabled = true;

            if (m_Animator != null) m_Animator.SetBool("isDead", false);

            // Cập nhật lại UI máu.
            SetHealthUI();
        }
    }
}