using UnityEngine;

[CreateAssetMenu(fileName = "Soldier", menuName = "Scriptable Objects/Soldier")]
public class Soldier : ScriptableObject
{
    [Header("Vida")]
    public float maxHealth = 50f;

    [Header("Movimiento")]
    public float moveSpeed = 3f;

    [Header("Detección (Cono)")]
    [Tooltip("Mitad del ángulo del cono (60 => total 120°)")]
    public float viewAngle = 60f;
    public float viewDistance = 8f;

    [Header("Detección - Raycast Obstrucción")]
    public float eyeHeight = 1.7f;
    public LayerMask obstructionMask;   // paredes / estructuras
}
