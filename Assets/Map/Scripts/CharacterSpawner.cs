using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    [Header("Nhân vật trong map")]
    public GameObject[] charactersInMap;

    [Header("Camera theo dõi")]
    public ThirdPersonCamera cameraFollow1;
    public ThirdPersonCamera cameraFollow2;

    public Camera player1Camera;
    public Camera player2Camera;

    void Start()
    {
        // Tắt tất cả nhân vật trước
        foreach (var character in charactersInMap)
        {
            if (character != null) character.SetActive(false);
        }

        GameObject player1 = null;
        GameObject player2 = null;

        // Chọn nhân vật cho Player 1
        if (GameSettings.PlayerCount >= 1 && GameSettings.Player1Character >= 0)
        {
            int index = GameSettings.Player1Character;
            player1 = charactersInMap[index];
            if (player1 != null)
            {
                player1.SetActive(true);

                var move = player1.GetComponent<Tanks.Complete.TankMovement>();
                if (move != null)
                {
                    move.ControlIndex = 1;
                    move.m_PlayerCamera = player1Camera;
                }
            }
        }

        // Chọn nhân vật cho Player 2
        if (GameSettings.PlayerCount >= 2 && GameSettings.Player2Character >= 0)
        {
            int index = GameSettings.Player2Character;
            player2 = charactersInMap[index];
            if (player2 != null)
            {
                player2.SetActive(true);

                var move = player2.GetComponent<Tanks.Complete.TankMovement>();
                if (move != null)
                {
                    move.ControlIndex = 2;
                    move.m_PlayerCamera = player2Camera;
                }
            }
        }

        // Gán target cho camera follow
        if (cameraFollow1 != null && player1 != null)
            cameraFollow1.target = player1.transform;

        if (cameraFollow2 != null && player2 != null)
            cameraFollow2.target = player2.transform;
    }
}
