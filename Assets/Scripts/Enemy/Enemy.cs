using System.Collections;
using UnityEngine;
using TMPro;

public class Enemy : MonoBehaviour
{
    public enum State { Normal, Chase, Damage, Dead }

    [Header("Config (ScriptableObject)")]
    public Soldier data;

    [Header("Estado actual")]
    public State state = State.Normal;

    [Header("Referencias")]
    public Transform player;
    PlayerStats playerStats;

    [Header("UI Estado")]
    public TextMeshPro stateText; // un TextMeshPro flotando sobre la cabeza del enemigo
    public float textHeight = 2f;

    float health;
    bool hasDetectedPlayer = false;  // IMPORTANTE → si detectó una vez, persigue siempre

    [SerializeField] float gravity = -9.81f;
    Vector3 verticalVelocity;    // para acumular caída

    void Start()
    {
        if (data == null)
        {
            Debug.LogError("No asignaste Soldier ScriptableObject");
            enabled = false;
            return;
        }

        health = data.maxHealth;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player != null) playerStats = player.GetComponent<PlayerStats>();

        UpdateStateUI();
    }

    void Update()
    {
        bool grounded = Physics.Raycast(
    transform.position + Vector3.up * 0.2f,
    Vector3.down,
    out RaycastHit groundHit,
    0.3f,
    LayerMask.GetMask("Default", "Environment")
);
        if (grounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f; // mismo truco que el player
        }
        else
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        if (state == State.Dead) return;

        // siempre mira la UI hacia la cámara
        if (stateText != null)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                stateText.transform.position = transform.position + Vector3.up * textHeight;
                stateText.transform.rotation = Quaternion.LookRotation(
                    stateText.transform.position - cam.transform.position
                );
            }
        }
        if (state == State.Damage)
        {
            // mostrar “damage” durante 0.1s y NO hacer nada más
            return;
        }
        // si ya detectó una vez → siempre persigue
        if (hasDetectedPlayer)
        {
            if (state != State.Chase) SetState(State.Chase);
            ChasePlayer();
            return;
        }

        // si todavía no detectó
        bool seesPlayer = CheckVision();

        if (state == State.Normal && seesPlayer)
        {
            hasDetectedPlayer = true;
            SetState(State.Chase);
        }

        if (state == State.Chase)
        {
            ChasePlayer();
        }
    }

    // --- MOVIMIENTO DE PERSECUCIÓN ---
    void ChasePlayer()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        Vector3 move = dir.normalized * data.moveSpeed * Time.deltaTime;

        // sumar gravedad
        move += verticalVelocity * Time.deltaTime;

        Vector3 nextPos = transform.position + move;

        // Si no hay colisión, moverse
        if (CanMoveTo(nextPos))
        {
            transform.position = nextPos;
        }
        else
        {
            // SLIDE: intentá moverte sin penetrar paredes
            // cancelamos X o Z según dónde esté bloqueado

            // probar mover solo en X
            Vector3 slideX = new Vector3(move.x, 0, 0);
            if (CanMoveTo(transform.position + slideX))
                transform.position += slideX;

            // probar mover solo en Z
            Vector3 slideZ = new Vector3(0, 0, move.z);
            if (CanMoveTo(transform.position + slideZ))
                transform.position += slideZ;
        }

        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    // --- VISIÓN: CONO + RAYCAST DE OBSTRUCCIÓN ---
    bool CheckVision()
    {
        if (player == null) return false;

        Vector3 eyePos = transform.position + Vector3.up * data.eyeHeight;
        Vector3 toPlayer = (player.position + Vector3.up * 1f) - eyePos;

        float dist = toPlayer.magnitude;
        if (dist > data.viewDistance) return false;

        Vector3 flatDir = toPlayer; flatDir.y = 0f;
        Vector3 forward = transform.forward; forward.y = 0f;
        float angle = Vector3.Angle(forward, flatDir);

        if (angle > data.viewAngle) return false;

        // RAYCAST → si golpea una pared ANTES que el jugador, NO lo ve
        if (Physics.Raycast(eyePos, toPlayer.normalized, out RaycastHit hit, data.viewDistance, data.obstructionMask))
        {
            // si pegó a algo antes de llegar al jugador → obstruido
            if (hit.collider.transform != player)
                return false;
        }

        return true;
    }

    // --- DAÑO ---
    public void TakeDamage(float dmg)
    {
        if (state == State.Dead) return;

        health -= dmg;
        if (health <= 0)
        {
            Die();
            return;
        }

        SetState(State.Damage);
        StopAllCoroutines();
        StartCoroutine(BackToChase());
    }

    IEnumerator BackToChase()
    {
        yield return new WaitForSeconds(0.1f);

        // si detectó una vez → siempre sigue en chase
        if (hasDetectedPlayer) SetState(State.Chase);
        else SetState(State.Normal);
    }

    void Die()
    {
        state = State.Dead;
        UpdateStateUI(); // muestra "dead"

        // Desactivar mesh del enemigo, pero NO el texto
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (r.gameObject != stateText.gameObject)
                r.enabled = false;
        }

        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        // Ocultar el texto después de 3 segundos
        StartCoroutine(HideStateTextAfterDelay());
    }

    // --- ESTADOS ---
    void SetState(State s)
    {
        state = s;
        UpdateStateUI();
    }

    void UpdateStateUI()
    {
        if (stateText != null)
        {
            stateText.text = state.ToString().ToLower();
        }
    }

    // --- DEBUG DEL CONO ---
    void OnDrawGizmosSelected()
    {
        if (data == null) return;

        Gizmos.color = Color.yellow;
        Vector3 eye = transform.position + Vector3.up * data.eyeHeight;

        // forward
        Gizmos.DrawLine(eye, eye + transform.forward * data.viewDistance);

        // límites del cono
        int steps = 20;
        for (int i = -steps; i <= steps; i++)
        {
            float t = (float)i / steps;
            float ang = t * data.viewAngle;
            Quaternion rot = Quaternion.AngleAxis(ang, Vector3.up);
            Vector3 dir = rot * transform.forward;
            Gizmos.DrawLine(eye, eye + dir.normalized * data.viewDistance);
        }
    }
    IEnumerator HideStateTextAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        if (stateText != null) stateText.gameObject.SetActive(false);
    }

    bool CanMoveTo(Vector3 targetPos)
    {
        // chequeo si hay algo entre el enemigo y el punto a mover
        if (Physics.CheckCapsule(
            transform.position + Vector3.up * 0.5f,
            transform.position + Vector3.up * 1.5f,
            0.35f,   // radio del capsule
            LayerMask.GetMask("Environment")
        ))
        {
            return false;
        }

        return true;
    }
}
