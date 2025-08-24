using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSettingsUI : MonoBehaviour
{
    [Header("Map Settings")]
    public Button[] mapButtons;
    public Image[] mapIcons;

    [Header("Player Count Settings")]
    public Button[] playerButtons;
    public Color selectedColor = Color.green;
    public Color defaultColor = Color.white;

    [Header("Match Count Settings")]
    public Button[] matchButtons;

    [Header("Character Settings")]
    public Button[] characterButtons;
    public Text[] characterLabels;
    public Button pickPlayer1Button;
    public Button pickPlayer2Button;
    public Color pickPlayerSelectedColor = Color.cyan;
    public Color disabledColor = Color.gray;

    public CharacterData[] characterDatas;
    public CharacterUI characterUI;

    private int currentPickingPlayer = 1;

    void Start()
    {
        for (int i = 0; i < mapButtons.Length; i++)
        {
            int index = i;
            mapButtons[i].onClick.AddListener(() => SelectMap(index));
        }

        for (int i = 0; i < playerButtons.Length; i++)
        {
            int index = i + 1;
            playerButtons[i].onClick.AddListener(() => SelectPlayerCount(index));
        }

        for (int i = 0; i < matchButtons.Length; i++)
        {
            int index = (i == 0) ? 3 : (i == 1 ? 5 : 7);
            matchButtons[i].onClick.AddListener(() => SelectMatchCount(index));
        }

        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i;
            characterButtons[i].onClick.AddListener(() => SelectCharacter(index));
        }

        pickPlayer1Button.onClick.AddListener(() => SetCurrentPickingPlayer(1));
        pickPlayer2Button.onClick.AddListener(() => SetCurrentPickingPlayer(2));

        ResetUI();
        LoadUIFromSettings();
    }

    void ResetUI()
    {
        foreach (var icon in mapIcons)
            icon.gameObject.SetActive(false);

        foreach (var btn in playerButtons)
            btn.image.color = defaultColor;

        foreach (var btn in matchButtons)
            btn.image.color = defaultColor;

        foreach (var lbl in characterLabels)
            lbl.text = "";

        pickPlayer1Button.image.color = defaultColor;
        pickPlayer2Button.image.color = defaultColor;
        pickPlayer2Button.interactable = true;
    }

    public void SelectMap(int index)
    {
        foreach (var icon in mapIcons)
            icon.gameObject.SetActive(false);

        mapIcons[index].gameObject.SetActive(true);
        GameSettings.SelectedMap = "Map_" + index;
    }

    public void SelectPlayerCount(int count)
    {
        foreach (var btn in playerButtons)
            btn.image.color = defaultColor;

        playerButtons[count - 1].image.color = selectedColor;
        GameSettings.PlayerCount = count;

        if (count == 1)
        {
            pickPlayer2Button.interactable = false;
            pickPlayer2Button.image.color = disabledColor;
            GameSettings.Player2Character = -1;
        }
        else
        {
            pickPlayer2Button.interactable = true;
            pickPlayer2Button.image.color = defaultColor;
        }
    }

    public void SelectMatchCount(int count)
    {
        foreach (var btn in matchButtons)
            btn.image.color = defaultColor;
        int buttonIndex = (count == 3) ? 0 : (count == 5 ? 1 : 2);
        matchButtons[buttonIndex].image.color = selectedColor;
        GameSettings.MatchCount = count;
    }


    void SetCurrentPickingPlayer(int player)
    {
        if (player == 2 && !pickPlayer2Button.interactable) return;

        currentPickingPlayer = player;
        pickPlayer1Button.image.color = (player == 1) ? pickPlayerSelectedColor : defaultColor;
        pickPlayer2Button.image.color = (player == 2) ? pickPlayerSelectedColor : defaultColor;
    }

    void SelectCharacter(int index)
    {
        if (currentPickingPlayer == 1)
        {
            GameSettings.Player1Character = index;
        }
        else
        {
            GameSettings.Player2Character = index;
        }
        characterUI.ShowCharacterInfo(characterDatas[index]);
        UpdateCharacterLabels();
    }

    void UpdateCharacterLabels()
    {
        for (int i = 0; i < characterLabels.Length; i++)
            characterLabels[i].text = "";

        if (GameSettings.Player1Character >= 0)
            characterLabels[GameSettings.Player1Character].text = "P1";

        if (GameSettings.Player2Character >= 0)
        {
            if (characterLabels[GameSettings.Player2Character].text == "P1")
                characterLabels[GameSettings.Player2Character].text = "P1, P2";
            else
                characterLabels[GameSettings.Player2Character].text = "P2";
        }
    }

    void LoadUIFromSettings()
    {
        if (!string.IsNullOrEmpty(GameSettings.SelectedMap))
        {
            string[] parts = GameSettings.SelectedMap.Split('_');
            if (parts.Length > 1 && int.TryParse(parts[1], out int mapIndex))
            {
                SelectMap(mapIndex);
            }
        }

        if (GameSettings.PlayerCount > 0)
        {
            SelectPlayerCount(GameSettings.PlayerCount);
        }

        if (GameSettings.MatchCount > 0)
        {
            SelectMatchCount(GameSettings.MatchCount);
        }
        UpdateCharacterLabels();
    }

    public bool CanGoToGameSettingTwo()
    {
        return !string.IsNullOrEmpty(GameSettings.SelectedMap)
            && GameSettings.PlayerCount > 0
            && GameSettings.MatchCount > 0;
    }

    public bool CanStartGame()
    {
        if (GameSettings.Player1Character < 0) return false;
        if (GameSettings.PlayerCount == 2 && GameSettings.Player2Character < 0) return false;
        return true;
    }

    public void GoToGame()
    {
        if (!CanStartGame())
        {
            Debug.LogWarning("Chưa chọn đầy đủ nhân vật!");
            return;
        }

        if (string.IsNullOrEmpty(GameSettings.SelectedMap))
        {
            Debug.LogWarning("Chưa chọn map!");
            return;
        }

        string[] parts = GameSettings.SelectedMap.Split('_');
        if (parts.Length > 1 && int.TryParse(parts[1], out int mapIndex))
        {
            switch (mapIndex)
            {
                case 0: SceneManager.LoadScene("DesertScene"); break;
                case 1: SceneManager.LoadScene("JungleHighLand"); break;
                default: Debug.LogWarning("Map chưa được gán scene!"); break;
            }
        }
    }
}
