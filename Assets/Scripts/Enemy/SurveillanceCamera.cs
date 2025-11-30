using UnityEngine;

public class SurveillanceCamera : MonoBehaviour
{
    [Header("Datos")]
    public SurveillanceCameraData data;

    [Header("Rotación")]
    public Transform pivot;              // Parte que rota
    public float rotationSpeed = 30f;    // Velocidad actual de giro
    public float bounceCheckDistance = 1.5f; // Distancia para detectar pared

    [Header("Jugador")]
    public Transform player;

    float health;

    void Start()
    {
        if (data == null)
        {
            Debug.LogError("No asignaste SurveillanceCameraData.");
            enabled = false;
            return;
        }

        if (pivot == null)
            pivot = transform;

        health = data.maxHealth;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        // Rotación que rebota al tocar paredes
        RotateByRaycastBounce();

        // Detección del jugador
        if (CanSeePlayer())
        {
            Debug.Log("📹 Cámara detectó al jugador → ALERTA GLOBAL");
            EnemyGlobalAlertSystem.AlertAllEnemies();
        }
    }

    // --- ROTACIÓN POR RAYCAST (REBOTE CON PAREDES) ---
    void RotateByRaycastBounce()
    {
        Vector3 origin = pivot.position + Vector3.up * data.eyeHeight;
        Vector3 forward = pivot.forward;

        // Solo chequeamos paredes (obstruction mask)
        if (Physics.Raycast(origin, forward, out RaycastHit hit, bounceCheckDistance, data.obstructionMask))
        {
            // Si choca contra una pared → invertimos la dirección del giro
            rotationSpeed = -rotationSpeed;
        }

        // Girar continuamente según la velocidad actual
        pivot.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
    }

    // --- VISIÓN: CONO + RAYCAST ---
    bool CanSeePlayer()
    {
        Vector3 origin = pivot.position + Vector3.up * data.eyeHeight;
        Vector3 toPlayer = (player.position + Vector3.up * 1f) - origin;

        // distancia
        float dist = toPlayer.magnitude;
        if (dist > data.viewDistance) return false;

        // ángulo
        Vector3 flatToPlayer = toPlayer; flatToPlayer.y = 0f;
        Vector3 forward = pivot.forward; forward.y = 0f;

        float angle = Vector3.Angle(forward, flatToPlayer);
        if (angle > data.viewAngle) return false;

        // raycast de obstrucción
        if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, data.viewDistance, data.obstructionMask))
        {
            if (hit.collider.transform != player)
                return false; // pegó contra algo que no es el player
        }

        return true;
    }

    // --- DAÑO ---
    public void TakeDamage(float dmg)
    {
        health -= dmg;
        if (health <= 0f)
        {
            Destroy(gameObject);
        }
    }

    // --- GIZMOS ---
    void OnDrawGizmosSelected()
    {
        if (data == null) return;

        Gizmos.color = Color.cyan;

        Vector3 origin = (pivot != null ? pivot.position : transform.position)
                         + Vector3.up * data.eyeHeight;

        int steps = 20;
        for (int i = -steps; i <= steps; i++)
        {
            float t = (float)i / steps;
            float ang = t * data.viewAngle;
            Quaternion rot = Quaternion.AngleAxis(ang, Vector3.up);
            Vector3 dir = rot * (pivot != null ? pivot.forward : transform.forward);
            Gizmos.DrawLine(origin, origin + dir.normalized * data.viewDistance);
        }
    }
}
