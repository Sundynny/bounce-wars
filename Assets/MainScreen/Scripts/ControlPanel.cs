using UnityEngine;

public class ControlPanel : MonoBehaviour
{
    public GameObject HomePanel;
    public GameObject GameSettingScreenOne;
    public GameObject GameSettingScreenTwo;
    public GameObject model3D;
    

    private GameObject[] allPanels;

    void Awake()
    {
        allPanels = new GameObject[] {
            HomePanel, GameSettingScreenOne, GameSettingScreenTwo};
    }

    private void ShowOnly(GameObject[] allPanels, GameObject currentPanel)
    {
        foreach (var panel in allPanels)
            panel.SetActive(false);

        currentPanel.SetActive(true);
    }

    public void ShowGameSettingOne()
    {
        model3D.SetActive(false);
        ShowOnly(allPanels, GameSettingScreenOne);
    }
    public void ShowGameSettingTwo()
    {
        model3D.SetActive(false);
        ShowOnly(allPanels, GameSettingScreenTwo);
    }
    
    public void BackToMenu()
    {
        model3D.SetActive(true);
        ShowOnly(allPanels, HomePanel);
    }

    void Start()
    {
        BackToMenu();
    }
}
