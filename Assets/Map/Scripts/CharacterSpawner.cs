using UnityEngine;
using Tanks.Complete;
using UnityEngine.UI;

public class CharacterSpawner : MonoBehaviour
{
    [Header("Prefabs nhân vật")]
    public GameObject[] characterPrefabs;

    [Header("Điểm spawn")]
    public Transform spawnPointPlayer1;
    public Transform spawnPointPlayer2;

    [Header("Camera")]
    public ThirdPersonCamera cameraP1;
    public ThirdPersonCamera cameraP2;

    [Header("UI Panel")]
    public GameObject abilityPanelP1;
    public GameObject abilityPanelP2;

    [Header("Player Cameras")]
    public Camera player1Camera;
    public Camera player2Camera;

    void Start()
    {
        SpawnPlayers();
    }

    void SpawnPlayers()
    {
        int charIndex1 = GameSettings.Player1Character;
        int charIndex2 = GameSettings.Player2Character;

        if (charIndex1 < 0 || charIndex1 >= characterPrefabs.Length ||
            charIndex2 < 0 || charIndex2 >= characterPrefabs.Length)
        {
            Debug.LogError("Chưa chọn nhân vật hợp lệ!");
            return;
        }

        // Spawn Player 1
        GameObject player1 = Instantiate(characterPrefabs[charIndex1], spawnPointPlayer1.position, spawnPointPlayer1.rotation);
        player1.name = "Player1";
        AssignPlayerData(player1, 1, Team.Team1, abilityPanelP1, player1Camera);
        cameraP1.target = player1.transform;

        // Spawn Player 2
        GameObject player2 = Instantiate(characterPrefabs[charIndex2], spawnPointPlayer2.position, spawnPointPlayer2.rotation);
        player2.name = "Player2";
        AssignPlayerData(player2, 2, Team.Team2, abilityPanelP2, player2Camera);
        cameraP2.target = player2.transform;
    }

    void AssignPlayerData(GameObject player, int playerNumber, Team team, GameObject abilityPanel, Camera assignedCamera)
    {
        // TankMovement: set PlayerNumber & Camera
        TankMovement move = player.GetComponent<TankMovement>();
        if (move != null)
        {
            move.m_PlayerNumber = playerNumber;

            // Gán PlayerCamera nếu có
            if (assignedCamera != null)
            {
                move.m_PlayerCamera = assignedCamera;
            }
        }

        // Gán Team
        TeamMember teamMember = player.GetComponent<TeamMember>();
        if (teamMember != null)
        {
            teamMember.Team = team;
        }

        // Gán Ability UI
        AbilityManager ability = player.GetComponent<AbilityManager>();
        if (ability != null)
        {
            ability.m_OrbCollector = player.GetComponent<OrbCollector>();
            ability.m_AbilityUI = abilityPanel.GetComponent<AbilityUI>();
        }
    }
}
