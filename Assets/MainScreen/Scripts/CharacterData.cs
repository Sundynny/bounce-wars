using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Game/CharacterData")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public int HP;
    public int Damage;
    public int Speed;
    [TextArea(3, 5)]
    public string description;
}
