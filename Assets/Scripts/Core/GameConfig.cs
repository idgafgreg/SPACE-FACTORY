using UnityEngine;

[CreateAssetMenu(menuName = "SpaceFactory/GameConfig", fileName = "GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Cycle Phases")]
    public float workDuration     = 60f;
    public float warningDuration  = 8f;
    public float defenseDuration  = 90f;
    public float recoveryDuration = 20f;

    [Header("Power")]
    public float startingMaxPower = 10f;

    [Header("Player")]
    public float playerMoveSpeed  = 5f;
    public float playerMaxHealth  = 100f;

    [Header("Starting Resources")]
    public int startingScrapMetal        = 30;
    public int startingEnergyCells       = 10;
    public int startingCircuitComponents = 5;
}
