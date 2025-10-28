using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStatsData", menuName = "Game/Player Stats Data")]
public class PlayerStatsData : ScriptableObject
{
    [Header("Vida")]
    public int maxHealth = 100;
    public int startHealth = 100;

    [Header("Estamina")]
    public int maxStamina = 10;
    public int startStamina = 10;
    [Tooltip("segundos por punto de estamina (1 = +1 por segundo)")]
    public float regenInterval = 1f;
}