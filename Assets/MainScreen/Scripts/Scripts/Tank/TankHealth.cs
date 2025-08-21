using UnityEngine;
using UnityEngine.UI;

namespace Tanks.Complete
{
    public class TankHealth : MonoBehaviour
    {
        // --- THÊM MỚI: Biến để tham chiếu đến Animator ---
        [Header("Animation Settings")]
        [Tooltip("Kéo Animator của nhân vật vào đây.")]
        public Animator m_Animator;
        [Tooltip("Thời gian (giây) chờ trước khi vô hiệu hóa GameObject sau khi chết. Cho phép animation chết phát hết.")]
        public float m_DeathDisableDelay = 2.5f;


        // --- CÁC BIẾN CŨ GIỮ NGUYÊN ---
        public float m_StartingHealth = 100f;
        public Slider m_Slider;
        public Image m_FillImage;
        public Color m_FullHealthColor = Color.green;
        public Color m_ZeroHealthColor = Color.red;
        public GameObject m_ExplosionPrefab;
        [HideInInspector] public bool m_HasShield;

        private AudioSource m_ExplosionAudio;
        private ParticleSystem m_ExplosionParticles;
        private float m_CurrentHealth;
        private bool m_Dead;
        private float m_ShieldValue;
        private bool m_IsInvincible;

        private void Awake()
        {
            m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();
            m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();
            m_ExplosionParticles.gameObject.SetActive(false);

            if (m_Animator == null)
            {
                m_Animator = GetComponentInChildren<Animator>();
            }

            if (m_Slider != null)
                m_Slider.maxValue = m_StartingHealth;
        }

        private void OnDestroy()
        {
            if (m_ExplosionParticles != null)
                Destroy(m_ExplosionParticles.gameObject);
        }

        private void OnEnable()
        {
            m_CurrentHealth = m_StartingHealth;
            m_Dead = false;
            m_HasShield = false;
            m_ShieldValue = 0;
            m_IsInvincible = false;

            if (m_Animator != null)
            {
                m_Animator.SetBool("isDead", false);
            }

            SetHealthUI();
        }

        // --- HÀM TakeDamage() ĐÃ ĐƯỢC NÂNG CẤP VÀ SỬA LỖI ---
        // Thêm một tham số bool mới: 'isDirectHit'
        public void TakeDamage(float amount, bool isDirectHit)
        {
            if (m_Dead) return;

            if (!m_IsInvincible)
            {
                m_CurrentHealth -= amount * (1 - m_ShieldValue);
                SetHealthUI();

                // Chỉ kích hoạt animation "Hurt" nếu đây là một cú đánh TRỰC TIẾP
                if (m_Animator != null && isDirectHit && m_CurrentHealth > 0f)
                {
                    m_Animator.SetTrigger("Hurt");
                }

                if (m_CurrentHealth <= 0f && !m_Dead)
                {
                    OnDeath();
                }
            }
        }

        public void IncreaseHealth(float amount)
        {
            if (m_CurrentHealth + amount <= m_StartingHealth)
            {
                m_CurrentHealth += amount;
            }
            else
            {
                m_CurrentHealth = m_StartingHealth;
            }
            SetHealthUI();
        }

        public void ToggleShield(float shieldAmount)
        {
            m_HasShield = !m_HasShield;
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
            if (m_Slider == null) return;
            m_Slider.value = m_CurrentHealth;
            m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
        }

        private void OnDeath()
        {
            m_Dead = true;

            if (m_Animator != null)
            {
                m_Animator.SetBool("isDead", true);
            }

            m_ExplosionParticles.transform.position = transform.position;
            m_ExplosionParticles.gameObject.SetActive(true);
            m_ExplosionParticles.Play();
            m_ExplosionAudio.Play();

            Invoke(nameof(DeactivateGameObject), m_DeathDisableDelay);
        }

        private void DeactivateGameObject()
        {
            gameObject.SetActive(false);
        }

        public void PlayCelebrateAnimation()
        {
            if (m_Animator != null)
            {
                m_Animator.SetTrigger("Celebrate");
            }
        }
    }
}