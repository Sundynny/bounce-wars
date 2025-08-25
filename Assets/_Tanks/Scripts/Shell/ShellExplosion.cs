using UnityEngine;
using System.Collections.Generic;

namespace Tanks.Complete
{
    public class ShellExplosion : MonoBehaviour
    {
        // Enum để xác định loại nguyên tố. Được gán bởi TankShooting.
        public enum ElementType { None, Fire, Water, Wind, Earth }

        [Header("Elemental Settings")]
        [Tooltip("Danh sách các nguyên tố được kích hoạt trên viên đạn này.")]
        [HideInInspector] public List<ElementType> m_ActiveElements = new List<ElementType>();

        [Header("Elemental Effect Prefabs")]
        [Tooltip("Prefab của hiệu ứng sẽ được tạo ra tại điểm nổ.")]
        public GameObject m_FireEffectPrefab;
        public GameObject m_WaterEffectPrefab;
        public GameObject m_WindEffectPrefab;
        public GameObject m_EarthEffectPrefab;

        [Header("Base Explosion Settings")]
        public LayerMask m_TankMask;
        public ParticleSystem m_ExplosionParticles;
        public AudioSource m_ExplosionAudio;
        [HideInInspector] public float m_MaxLifeTime = 2f;
        [HideInInspector] public float m_MaxDamage = 100f;
        [HideInInspector] public float m_ExplosionForce = 1000f;
        [HideInInspector] public float m_ExplosionRadius = 5f;
        [HideInInspector] public GameObject m_Owner;


        private void Start()
        {
            // Tự hủy viên đạn nếu nó không va chạm vào đâu sau một khoảng thời gian.
            Destroy(gameObject, m_MaxLifeTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Ghi nhớ mục tiêu bị trúng trực tiếp để xử lý animation.
            TankHealth directHitTarget = other.GetComponent<TankHealth>();

            // 1. TÍNH TOÁN CÁC CHỈ SỐ VỤ NỔ DỰA TRÊN NGUYÊN TỐ
            //----------------------------------------------------
            float currentExplosionRadius = m_ExplosionRadius;
            float currentMaxDamage = m_MaxDamage;
            float currentExplosionForce = m_ExplosionForce;

            // Duyệt qua danh sách các nguyên tố được gán cho viên đạn.
            foreach (var element in m_ActiveElements)
            {
                switch (element)
                {
                    case ElementType.Fire:
                        // Lửa tăng sát thương.
                        currentMaxDamage *= 1.5f;
                        break;
                    case ElementType.Wind:
                        // Gió tăng phạm vi và lực đẩy.
                        currentExplosionRadius *= 1.5f;
                        currentExplosionForce *= 2f;
                        break;
                        // Các hiệu ứng của Nước và Đất sẽ do Prefab của chúng tự xử lý.
                        // Chúng không làm thay đổi các chỉ số của vụ nổ ban đầu.
                }
            }

            // 2. GÂY SÁT THƯƠNG VÀ LỰC ĐẨY (LOGIC CỦA VỤ NỔ)
            //----------------------------------------------------
            Collider[] colliders = Physics.OverlapSphere(transform.position, currentExplosionRadius, m_TankMask);

            for (int i = 0; i < colliders.Length; i++)
            {
                Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();
                if (!targetRigidbody) continue;

                TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();
                if (!targetHealth || targetHealth.gameObject == m_Owner) continue;

                // Áp dụng lực đẩy đã được cường hóa.
                float actualForce = CalculateForce(targetRigidbody.position, currentExplosionRadius, currentExplosionForce);
                targetRigidbody.AddExplosionForce(actualForce, transform.position, currentExplosionRadius);

                // Gây sát thương đã được cường hóa.
                float damage = CalculateDamage(targetRigidbody.position, currentExplosionRadius, currentMaxDamage);
                bool isDirectHit = (targetHealth == directHitTarget);
                targetHealth.TakeDamage(damage, isDirectHit);
            }

            // 3. TRIỆU HỒI CÁC PREFAB HIỆU ỨNG TƯƠNG ỨNG
            //----------------------------------------------------
            foreach (var element in m_ActiveElements)
            {
                switch (element)
                {
                    case ElementType.Fire:
                        if (m_FireEffectPrefab != null)
                            Instantiate(m_FireEffectPrefab, transform.position, Quaternion.identity);
                        break;
                    case ElementType.Wind:
                        if (m_WindEffectPrefab != null)
                            Instantiate(m_WindEffectPrefab, transform.position, Quaternion.identity);
                        break;
                    case ElementType.Water:
                        Debug.Log("Đạn Nước đã nổ! Chuẩn bị triệu hồi WaterEffectPrefab.");
                        if (m_WaterEffectPrefab != null)
                        Instantiate(m_WaterEffectPrefab, transform.position, Quaternion.identity);
                        break;
                    case ElementType.Earth:
                        if (m_EarthEffectPrefab != null)
                            Instantiate(m_EarthEffectPrefab, transform.position, Quaternion.identity);
                        break;
                }
            }

            // 4. KÍCH HOẠT HIỆU ỨNG NỔ GỐC VÀ TỰ HỦY
            //----------------------------------------------------
            Explode();
        }

        private void Explode()
        {
            // Tách hiệu ứng nổ gốc ra khỏi viên đạn.
            m_ExplosionParticles.transform.parent = null;
            m_ExplosionParticles.Play();
            m_ExplosionAudio.Play();

            // Hủy các đối tượng.
            ParticleSystem.MainModule mainModule = m_ExplosionParticles.main;
            Destroy(m_ExplosionParticles.gameObject, mainModule.duration);
            Destroy(gameObject);
        }

        // Các hàm tính toán phụ trợ.
        private float CalculateDamage(Vector3 targetPosition, float radius, float maxDamage)
        {
            Vector3 explosionToTarget = targetPosition - transform.position;
            float explosionDistance = explosionToTarget.magnitude;
            float relativeDistance = (radius - explosionDistance) / radius;
            float damage = relativeDistance * maxDamage;
            return Mathf.Max(0f, damage);
        }

        private float CalculateForce(Vector3 targetPosition, float radius, float maxForce)
        {
            Vector3 explosionToTarget = targetPosition - transform.position;
            float distance = explosionToTarget.magnitude;
            float distanceProportion = (radius - distance) / radius;
            return Mathf.Max(0f, distanceProportion * maxForce);
        }
    }
}