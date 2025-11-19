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
    bool hasDetectedPlayer = false;  // si detectó una vez, persigue siempre

    // Rigidbody
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Enemy necesita un Rigidbody en el mismo GameObject.");
        }
    }

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
        if (state == State.Dead)
        {
            // solo mantenemos el texto mirando a la cámara
            FaceTextToCamera();
            return;
        }

        // siempre mira la UI hacia la cámara
        FaceTextToCamera();

        // si está en Damage, no hacemos lógica de IA
        if (state == State.Damage)
            return;

        // si ya detectó una vez → siempre persigue
        if (hasDetectedPlayer)
        {
            if (state != State.Chase) SetState(State.Chase);
            return; // el movimiento lo hacemos en FixedUpdate
        }

        // si todavía no detectó
        bool seesPlayer = CheckVision();

        if (state == State.Normal && seesPlayer)
        {
            hasDetectedPlayer = true;
            SetState(State.Chase);
        }
    }

    void FixedUpdate()
    {
        if (state == State.Chase && player != null && state != State.Dead)
        {
            ChasePlayer();
        }
    }

    void FaceTextToCamera()
    {
        if (stateText == null) return;

        var cam = Camera.main;
        if (cam != null)
        {
            stateText.transform.position = transform.position + Vector3.up * textHeight;
            stateText.transform.rotation = Quaternion.LookRotation(
                stateText.transform.position - cam.transform.position
            );
        }
    }

    // --- MOVIMIENTO DE PERSECUCIÓN (con Rigidbody) ---
    void ChasePlayer()
    {
        if (rb == null || player == null) return;

        // Vector hacia el jugador PERMANENTE y sin frenar
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f; // no saltar

        // si está extremadamente cerca, seguir igualmente
        Vector3 dir = toPlayer.normalized;

        // velocidad constante hacia el jugador
        rb.linearVelocity = new Vector3(dir.x * data.moveSpeed, rb.linearVelocity.y, dir.z * data.moveSpeed);

        // rota hacia el jugador
        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
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
            if (stateText != null && r.gameObject == stateText.gameObject) continue;
            r.enabled = false;
        }

        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

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
}