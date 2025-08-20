using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    [Header("Prefabs nhân vật")]
    public GameObject[] characterPrefabs;

    [Header("Điểm spawn")]
    public Transform player1Spawn;
    public Transform player2Spawn;

    public ThirdPersonCamera cameraFollow1; // Camera cho player 1
    public ThirdPersonCamera cameraFollow2; // Camera cho player 2

    void Start()
    {
        GameObject player1 = null;
        GameObject player2 = null;

        // Spawn player 1
        if (GameSettings.PlayerCount >= 1 && GameSettings.Player1Character >= 0)
        {
            int index = GameSettings.Player1Character;
            player1 = Instantiate(characterPrefabs[index], player1Spawn.position, player1Spawn.rotation);

            // Gán control index = 1 (Keyboard Left)
            var move = player1.GetComponent<Tanks.Complete.TankMovement>();
            if (move != null) move.ControlIndex = 1;
        }

        // Spawn player 2
        if (GameSettings.PlayerCount >= 2 && GameSettings.Player2Character >= 0)
        {
            int index = GameSettings.Player2Character;
            player2 = Instantiate(characterPrefabs[index], player2Spawn.position, player2Spawn.rotation);

            // Gán control index = 2 (Keyboard Right)
            var move = player2.GetComponent<Tanks.Complete.TankMovement>();
            if (move != null) move.ControlIndex = 2;
        }

        // Gán camera cho từng player
        if (cameraFollow1 != null && player1 != null)
            cameraFollow1.target = player1.transform;

        if (cameraFollow2 != null && player2 != null)
            cameraFollow2.target = player2.transform;
    }
}
