using UnityEngine;
using UnityEngine.UI;

public class CharacterUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Text nameText;
    public Text hpText;
    public Text damageText;
    public Text speedText;
    public Text descriptionText;

    public void ShowCharacterInfo(CharacterData data)
    {
        nameText.text = data.characterName;
        hpText.text = "" + data.HP;
        damageText.text = "" + data.Damage;
        speedText.text = "" + data.Speed;
        descriptionText.text = data.description;
    }
}
