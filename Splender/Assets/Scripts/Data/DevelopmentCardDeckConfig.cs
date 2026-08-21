using UnityEngine;

[CreateAssetMenu(fileName = "DevelopmentCardDeckConfig", menuName = "Config/Development Card Deck")]
public class DevelopmentCardDeckConfig : ScriptableObject
{
    public int level;
    public DevelopmentCardData[] cards;
}
