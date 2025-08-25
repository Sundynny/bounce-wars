using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic; // Cần dùng để có List

namespace Tanks.Complete
{
    // Yêu cầu phải có OrbCollector trên cùng GameObject để hoạt động
    [RequireComponent(typeof(OrbCollector))]
    public class AbilityManager : MonoBehaviour
    {
        [Header("Component References")]
        [Tooltip("Tham chiếu đến script OrbCollector. Sẽ tự động lấy nếu bỏ trống.")]
        public OrbCollector m_OrbCollector;
        [Tooltip("Tham chiếu đến script AbilityUI để cập nhật giao diện.")]
        public AbilityUI m_AbilityUI; // Chúng ta sẽ tạo script này sau

        // --- BIẾN NỘI BỘ ---
        private TankInputUser m_InputUser;

        // Các hành động (Actions) cho từng kỹ năng
        private InputAction m_fireAbilityAction;
        private InputAction m_waterAbilityAction;
        private InputAction m_windAbilityAction;
        private InputAction m_earthAbilityAction;

        // Lưu trữ loại nguyên tố đang được "chọn" để cường hóa
        private PowerOrbController.ElementType m_SelectedEmpowerment = PowerOrbController.ElementType.None;

        private void Awake()
        {
            // Lấy các component cần thiết
            m_InputUser = GetComponent<TankInputUser>();
            if (m_OrbCollector == null)
            {
                m_OrbCollector = GetComponent<OrbCollector>();
            }
        }

        private void Start()
        {
            // Lấy và kích hoạt các hành động kỹ năng từ Input Actions Asset
            // LƯU Ý: Bạn cần tạo các Action này trong file Input Actions của mình!
            m_fireAbilityAction = m_InputUser.ActionAsset.FindAction("AbilityFire");
            m_waterAbilityAction = m_InputUser.ActionAsset.FindAction("AbilityWater");
            m_windAbilityAction = m_InputUser.ActionAsset.FindAction("AbilityWind");
            m_earthAbilityAction = m_InputUser.ActionAsset.FindAction("AbilityEarth");

            m_fireAbilityAction?.Enable();
            m_waterAbilityAction?.Enable();
            m_windAbilityAction?.Enable();
            m_earthAbilityAction?.Enable();

            // Cập nhật UI lần đầu
            UpdateAvailableAbilitiesUI();
        }

        private void Update()
        {
            // Lắng nghe input của người chơi để chọn kỹ năng
            if (m_fireAbilityAction != null && m_fireAbilityAction.WasPressedThisFrame())
            {
                SelectEmpowerment(PowerOrbController.ElementType.Fire);
            }
            if (m_waterAbilityAction != null && m_waterAbilityAction.WasPressedThisFrame())
            {
                SelectEmpowerment(PowerOrbController.ElementType.Water);
            }
            if (m_windAbilityAction != null && m_windAbilityAction.WasPressedThisFrame())
            {
                SelectEmpowerment(PowerOrbController.ElementType.Wind);
            }
            if (m_earthAbilityAction != null && m_earthAbilityAction.WasPressedThisFrame())
            {
                SelectEmpowerment(PowerOrbController.ElementType.Earth);
            }
        }

        // Hàm chọn một nguyên tố để cường hóa
        private void SelectEmpowerment(PowerOrbController.ElementType type)
        {
            // Kiểm tra xem người chơi có thực sự đang giữ quả cầu loại đó không
            if (m_OrbCollector.GetCollectedOrbTypes().Contains(type))
            {
                // Nếu đang chọn chính nó, thì bỏ chọn (toggle off)
                if (m_SelectedEmpowerment == type)
                {
                    m_SelectedEmpowerment = PowerOrbController.ElementType.None;
                }
                else // Nếu chọn một kỹ năng mới
                {
                    m_SelectedEmpowerment = type;
                }

                // Cập nhật lại UI để hiển thị kỹ năng đã được chọn
                if (m_AbilityUI != null)
                {
                    m_AbilityUI.SetSelectedAbility(m_SelectedEmpowerment);
                }
            }
        }

        /// <summary>
        /// Được gọi bởi TankShooting để biết cần bắn loại đạn nào.
        /// </summary>
        public PowerOrbController.ElementType GetSelectedEmpowerment()
        {
            return m_SelectedEmpowerment;
        }

        /// <summary>
        /// Được gọi bởi TankShooting sau khi đã bắn.
        /// </summary>
        // Sửa lại hàm này trong AbilityManager.cs
        public void OnFireComplete()
        {
            if (m_SelectedEmpowerment != PowerOrbController.ElementType.None)
            {
                // --- THAY ĐỔI: Ra lệnh cho OrbCollector tiêu thụ đúng quả cầu đã chọn ---
                m_OrbCollector.ConsumeOrb(m_SelectedEmpowerment);

                m_SelectedEmpowerment = PowerOrbController.ElementType.None;

                // Cập nhật lại toàn bộ UI sau khi đã tiêu thụ
                UpdateAvailableAbilitiesUI();
            }
        }

        /// <summary>
        /// Cập nhật UI dựa trên các quả cầu đang có.
        /// Sẽ được gọi khi bắt đầu và mỗi khi nhặt/tiêu thụ quả cầu.
        /// </summary>
        public void UpdateAvailableAbilitiesUI()
        {
            if (m_AbilityUI != null)
            {
                // Lấy danh sách các quả cầu hiện có và gửi cho UI để làm sáng các icon tương ứng
                m_AbilityUI.HighlightAvailableAbilities(m_OrbCollector.GetCollectedOrbTypes());
                // Đảm bảo không có kỹ năng nào được chọn
                m_AbilityUI.SetSelectedAbility(PowerOrbController.ElementType.None);
            }
        }
    }
}