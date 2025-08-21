using UnityEngine;

namespace Tanks.Complete
{
    public class ShellExplosion : MonoBehaviour
    {
        public LayerMask m_TankMask;
        public ParticleSystem m_ExplosionParticles;
        public AudioSource m_ExplosionAudio;
        [HideInInspector] public float m_MaxLifeTime = 2f;
        [HideInInspector] public float m_MaxDamage = 100f;
        [HideInInspector] public float m_ExplosionForce = 1000f; // Đây là lực đẩy TỐI ĐA ở tâm
        [HideInInspector] public float m_ExplosionRadius = 5f;
        [HideInInspector] public GameObject m_Owner;


        private void Start()
        {
            Destroy(gameObject, m_MaxLifeTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            TankHealth directHitTarget = other.GetComponent<TankHealth>();

            Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_TankMask);

            for (int i = 0; i < colliders.Length; i++)
            {
                Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();
                if (!targetRigidbody) continue;

                TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();
                if (!targetHealth) continue;

                if (targetHealth.gameObject == m_Owner) continue;

                // --- LOGIC LỰC ĐẨY ĐÃ ĐƯỢC NÂNG CẤP ---

                // 1. Tính toán khoảng cách từ tâm nổ đến mục tiêu
                Vector3 explosionToTarget = targetRigidbody.position - transform.position;
                float distance = explosionToTarget.magnitude;

                // 2. Tính toán tỷ lệ khoảng cách (0 = ở rìa, 1 = ở tâm)
                float distanceProportion = (m_ExplosionRadius - distance) / m_ExplosionRadius;

                // 3. Tính toán lực đẩy thực tế dựa trên tỷ lệ đó
                // Đảm bảo lực đẩy không bao giờ là số âm
                float actualForce = Mathf.Max(0f, distanceProportion * m_ExplosionForce);

                // 4. Tác động lực đẩy đã được tính toán
                targetRigidbody.AddExplosionForce(actualForce, transform.position, m_ExplosionRadius);


                // --- Logic gây sát thương và animation giữ nguyên ---
                float damage = CalculateDamage(targetRigidbody.position);
                bool isDirectHit = (targetHealth == directHitTarget);
                targetHealth.TakeDamage(damage, isDirectHit);
            }

            Explode();
        }

        private void Explode()
        {
            m_ExplosionParticles.transform.parent = null;
            m_ExplosionParticles.Play();
            m_ExplosionAudio.Play();
            ParticleSystem.MainModule mainModule = m_ExplosionParticles.main;
            Destroy(m_ExplosionParticles.gameObject, mainModule.duration);
            Destroy(gameObject);
        }

        private float CalculateDamage(Vector3 targetPosition)
        {
            Vector3 explosionToTarget = targetPosition - transform.position;
            float explosionDistance = explosionToTarget.magnitude;
            float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;
            float damage = relativeDistance * m_MaxDamage;
            damage = Mathf.Max(0f, damage);
            return damage;
        }
    }
}