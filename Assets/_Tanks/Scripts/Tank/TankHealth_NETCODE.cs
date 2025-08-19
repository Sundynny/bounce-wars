using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace Tanks.Complete
{
    public class TankHealth_NETCOODE : NetworkBehaviour
    {
        // --- Tất cả các biến và các hàm Awake, OnDestroy, OnNetworkSpawn, OnNetworkDespawn, OnHealthChanged giữ nguyên ---
        public float m_StartingHealth = 100f;
        public Slider m_Slider;
        public Image m_FillImage;
        public Color m_FullHealthColor = Color.green;
        public Color m_ZeroHealthColor = Color.red;
        public GameObject m_ExplosionPrefab;
        [HideInInspector] public bool m_HasShield;
        private AudioSource m_ExplosionAudio;
        private ParticleSystem m_ExplosionParticles;
        private NetworkVariable<float> m_CurrentHealth = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Server);
        private bool m_Dead;
        private float m_ShieldValue;
        private bool m_IsInvincible;

        private void Awake()
        {
            m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();
            m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();
            m_ExplosionParticles.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (m_ExplosionParticles != null) Destroy(m_ExplosionParticles.gameObject);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                m_CurrentHealth.Value = m_StartingHealth;
            }
            m_CurrentHealth.OnValueChanged += OnHealthChanged;
            m_Dead = false;
            m_HasShield = false;
            m_ShieldValue = 0;
            m_IsInvincible = false;
            SetHealthUI(m_CurrentHealth.Value);
        }

        public override void OnNetworkDespawn()
        {
            m_CurrentHealth.OnValueChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(float previousValue, float newValue)
        {
            SetHealthUI(newValue);
        }

        public void TakeDamage(float amount)
        {
            TakeDamageServerRpc(amount);
        }

        [ServerRpc(RequireOwnership = false)]
        private void TakeDamageServerRpc(float amount)
        {
            if (m_Dead) return;
            if (!m_IsInvincible)
            {
                m_CurrentHealth.Value -= amount * (1 - m_ShieldValue);
            }
            if (m_CurrentHealth.Value <= 0f && !m_Dead)
            {
                OnDeath();
            }
        }

        public void IncreaseHealth(float amount)
        {
            if (!IsServer) return;
            if (m_CurrentHealth.Value + amount <= m_StartingHealth)
            {
                m_CurrentHealth.Value += amount;
            }
            else
            {
                m_CurrentHealth.Value = m_StartingHealth;
            }
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

        private void SetHealthUI(float currentHealth)
        {
            m_Slider.value = currentHealth;
            m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, currentHealth / m_StartingHealth);
        }

        // OnDeath bây giờ là hàm logic TRÊN SERVER để bắt đầu quá trình chết
        private void OnDeath()
        {
            // Đảm bảo hàm này chỉ chạy trên Server.
            if (!IsServer) return;

            // Đặt cờ để hàm này chỉ được gọi một lần.
            m_Dead = true;

            // --- ĐOẠN MÃ ĐÃ ĐƯỢC DI CHUYỂN LÊN ĐÂY ---
            // Server xử lý logic rơi vật phẩm trước khi ra lệnh cho client hiển thị hiệu ứng.
            TankFlagCarrier_NETCODES carrier = GetComponent<TankFlagCarrier_NETCODES>();
            if (carrier != null)
            {
                // Gọi hàm để thả tất cả các vật phẩm mà xe tăng này đang mang.
                // Hàm này chỉ chạy trên server nên hoàn toàn an toàn.
                carrier.DropAllPointsOnDeath();
            }

            // Sau khi xử lý logic xong, Server gọi một ClientRpc để tất cả các client cùng hiển thị hiệu ứng chết.
            OnDeathClientRpc();
        }

        // [ClientRpc] - Hàm này được Server gọi, và thực thi trên TẤT CẢ các Client.
        // Nó chỉ chịu trách nhiệm cho HIỆU ỨNG HÌNH ẢNH VÀ ÂM THANH.
        [ClientRpc]
        private void OnDeathClientRpc()
        {
            // Phát hiệu ứng nổ
            m_ExplosionParticles.transform.position = transform.position;
            m_ExplosionParticles.gameObject.SetActive(true);
            m_ExplosionParticles.Play();
            m_ExplosionAudio.Play();

            // Tắt các thành phần để xe tăng trông như đã bị phá hủy
            var tankRenderer = GetComponent<Renderer>();
            if (tankRenderer != null)
            {
                tankRenderer.enabled = false;
            }

            var tankCollider = GetComponent<Collider>();
            if (tankCollider != null)
            {
                tankCollider.enabled = false;
            }

            var tankMovement = GetComponent<TankMovement>();
            if (tankMovement != null)
            {
                tankMovement.enabled = false;
            }

            var tankShooting = GetComponent<TankShooting>();
            if (tankShooting != null)
            {
                tankShooting.enabled = false;
            }
        }
    }
}