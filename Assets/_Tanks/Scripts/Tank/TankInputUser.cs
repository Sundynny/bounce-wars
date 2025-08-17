using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Tanks.Complete
{
    /// <summary>
    /// Chứa Người dùng đầu vào (Input User) của Hệ thống đầu vào (Input System) được liên kết với một Xe tăng. 
    /// Thành phần này đảm nhiệm việc sao chép các hành động đầu vào (input actions) mặc định từ Cài đặt dự án (Project Settings) và liên kết chúng với Người dùng đầu vào đã cho. 
    /// Điều này là cần thiết vì nếu không, các hành động đầu vào trên toàn dự án sẽ liên tục bị ghi đè bởi bất kỳ ai liên kết với chúng sau cùng.
    /// </summary>
    public class TankInputUser : MonoBehaviour
    {
        public InputUser InputUser => m_InputUser;                      // Người dùng đầu vào (InputUser) cho xe tăng này
        public InputActionAsset ActionAsset => m_LocalActionAsset;      // Bản sao Tài sản Hành động Đầu vào (Input Action Asset) cục bộ chỉ được liên kết với thiết bị phù hợp

        private InputUser m_InputUser;
        private InputActionAsset m_LocalActionAsset;

        private void Awake()
        {
            // Sao chép Sơ đồ Hành động (Action Map) để các hành động có thể được ghép nối với một thiết bị cụ thể (nếu không, các hành động mặc định
            // sẽ bị một thiết bị chiếm dụng và sau đó không khả dụng cho bất kỳ người chơi nào khác)
            m_LocalActionAsset = InputActionAsset.FromJson(InputSystem.actions.ToJson());

            // Theo mặc định, ghép nối với bàn phím, vì đây là phương thức nhập liệu mặc định. Điều này cho phép nó hoạt động ngay cả khi không
            // có menu để gán bất kỳ chế độ nhập liệu nào khác.
            SetNewInputUser(InputUser.PerformPairingWithDevice(Keyboard.current));
        }

        /// <summary>
        /// Kích hoạt sơ đồ điều khiển (control scheme) đã cho trên Người dùng đầu vào (Input User)
        /// </summary>
        /// <param name="name">Tên của Sơ đồ điều khiển (ControlScheme) cần kích hoạt</param>
        public void ActivateScheme(string name)
        {
            m_InputUser.ActivateControlScheme(name);
        }

        /// <summary>
        /// Thay thế người dùng đầu vào (input user) chứa trong thành phần này bằng người dùng đã cho
        /// </summary>
        /// <param name="user">Người dùng đầu vào (InputUser) mới</param>
        public void SetNewInputUser(InputUser user)
        {
            if (!user.valid)
                return;

            m_InputUser = user;
            m_InputUser.AssociateActionsWithUser(m_LocalActionAsset);

            // Nếu người dùng này có một sơ đồ điều khiển liên quan (ví dụ: trong dự án này là KeyboardRight hoặc KeyboardLeft), chúng tôi
            // sẽ kích hoạt lại sơ đồ này trên người dùng đầu vào. Điều này là cần thiết vì chúng tôi đã thay đổi các hành động liên quan ở dòng trên,
            // do đó các hành động mới đó chưa được thiết lập sơ đồ điều khiển, và dòng này sẽ thiết lập nó.
            if (m_InputUser.controlScheme.HasValue)
                m_InputUser.ActivateControlScheme(m_InputUser.controlScheme.Value);
        }
    }
}