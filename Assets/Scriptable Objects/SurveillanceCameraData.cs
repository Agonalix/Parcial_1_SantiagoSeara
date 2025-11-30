using UnityEngine;

[CreateAssetMenu(fileName = "SurveillanceCamera", menuName = "Scriptable Objects/Surveillance Camera")]
public class SurveillanceCameraData : ScriptableObject
{
    [Header("Vida")]
    public float maxHealth = 100f;

    [Header("Visión")]
    [Tooltip("Mitad del ángulo del cono (60 => total 120°)")]
    public float viewAngle = 60f;

    [Tooltip("Alcance de visión en metros")]
    public float viewDistance = 5f;

    [Header("Altura del ojo")]
    public float eyeHeight = 1.7f;

    [Header("Obstrucciones")]
    public LayerMask obstructionMask;   // paredes / estructuras
}
