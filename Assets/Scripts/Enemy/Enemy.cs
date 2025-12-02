using System.Collections;
using UnityEngine;
using TMPro;

public class Enemy : MonoBehaviour
{
    public enum State { Normal, Patrol, Alert, Chase, Damage, Dead }

    [Header("Config (ScriptableObject)")]
    public Soldier data;

    [Header("Estado actual")]
    public State state = State.Normal;

    [Header("Referencias")]
    public Transform player;
    PlayerStats playerStats;

    [Header("Patrulla")]
    public PatrolPath patrolPath;
    public int patrolIndex = 0;
    public float patrolSpeed = 2f;
    public float patrolReachDistance = 0.3f;

    [Header("UI Estado")]
    public TextMeshPro stateText;
    public float textHeight = 2f;

    [Header("Ataque (a distancia)")]
    public float attackDamage = 10f;
    public float attackRate = 1.2f;
    float nextAttackTime;

    float health;

    // Si detectó al jugador (visión o daño)
    bool hasDetectedPlayer = false;

    // Si este enemigo ya recibió la alerta global
    bool isGloballyAlerted = false;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        EnemyGlobalAlertSystem.Register(this);
    }

    void OnDestroy()
    {
        EnemyGlobalAlertSystem.Unregister(this);
    }

    void Start()
    {
        rb.freezeRotation = true;

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

        if (player != null)
            playerStats = player.GetComponent<PlayerStats>();

        UpdateStateUI();
    }

    void Update()
    {
        if (state == State.Dead)
        {
            FaceTextToCamera();
            return;
        }

        FaceTextToCamera();

        if (state == State.Damage)
            return;

        // SI ESTÁ EN ALERTA GLOBAL → Persigue SIEMPRE
        if (isGloballyAlerted)
        {
            if (state != State.Alert && state != State.Chase)
                SetState(State.Alert);

            return;
        }

        // Si detectó localmente → Chase
        if (hasDetectedPlayer)
        {
            if (state != State.Chase)
                SetState(State.Chase);

            return;
        }

        // VISIÓN
        bool seesPlayer = CheckVision();

        if (seesPlayer)
        {
            hasDetectedPlayer = true;

            // ALERTA GLOBAL INMEDIATA
            EnemyGlobalAlertSystem.AlertAllEnemies();
            Debug.Log("👀 Soldier vio al jugador → ALERTA GLOBAL");

            return;
        }

        // PATRULLA
        if (state == State.Patrol)
            PatrolLogic();
    }

    void FixedUpdate()
    {
        if ((state == State.Chase || state == State.Alert) && player != null)
        {
            TryAttack();
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
            stateText.transform.rotation =
                Quaternion.LookRotation(stateText.transform.position - cam.transform.position);
        }
    }

    // ===== PERSECUCIÓN =====
    void ChasePlayer()
    {
        if (rb == null || player == null) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0;
        Vector3 dir = toPlayer.normalized;

        rb.linearVelocity = new Vector3(
            dir.x * data.moveSpeed,
            rb.linearVelocity.y,
            dir.z * data.moveSpeed
        );

        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    // ===== VISIÓN =====
    bool CheckVision()
    {
        if (player == null) return false;

        Vector3 eyePos = transform.position + Vector3.up * data.eyeHeight;
        Vector3 toPlayer = (player.position + Vector3.up) - eyePos;

        float dist = toPlayer.magnitude;
        if (dist > data.viewDistance) return false;

        Vector3 flatDir = toPlayer; flatDir.y = 0;
        Vector3 forward = transform.forward; forward.y = 0;

        float angle = Vector3.Angle(forward, flatDir);
        if (angle > data.viewAngle) return false;

        if (Physics.Raycast(eyePos, toPlayer.normalized,
            out RaycastHit hit, data.viewDistance, data.obstructionMask))
        {
            if (hit.collider.transform != player)
                return false;
        }

        return true;
    }

    // ===== DAÑO =====
    public void TakeDamage(float dmg)
    {
        if (state == State.Dead) return;

        StopAllCoroutines();
        health -= dmg;

        if (health <= 0)
        {
            Die();
            return;
        }

        // --- FIX PARA EL TROPIEZO ---
        rb.linearVelocity = Vector3.zero; // frena al instante

        // Si quisieras ser más estricto:
        rb.angularVelocity = Vector3.zero;

        // LOCAL ALERTA (solo este enemigo)
        hasDetectedPlayer = true;

        // Estado de daño
        SetState(State.Damage);

        // Timer de 3s para alerta global
        StartCoroutine(AlertIfNotKilledSoon());

        // Vuelve a chase o patrol
        StartCoroutine(BackToChase());
    }


    IEnumerator BackToChase()
    {
        yield return new WaitForSeconds(0.25f);

        if (isGloballyAlerted)
            SetState(State.Alert);
        else if (hasDetectedPlayer)
            SetState(State.Chase);
        else
            SetState(State.Patrol);
    }

    void Die()
    {
        state = State.Dead;
        UpdateStateUI();

        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (stateText != null && r.gameObject == stateText.gameObject) continue;
            r.enabled = false;
        }

        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        rb.useGravity = false;
        rb.isKinematic = true;

        StartCoroutine(HideStateTextAfterDelay());
    }

    // ===== ALERTA GLOBAL =====
    public void ForceAlert()
    {
        if (state == State.Dead) return;

        isGloballyAlerted = true;
        hasDetectedPlayer = true;

        SetState(State.Alert);
    }

    IEnumerator AlertIfNotKilledSoon()
    {
        yield return new WaitForSeconds(3f);

        // no murió → ALERTA GLOBAL
        if (!isGloballyAlerted && health > 0)
        {
            EnemyGlobalAlertSystem.AlertAllEnemies();
            Debug.Log("⚠ ALERTA GLOBAL por enemigo herido");
        }
    }

    // ===== ATAQUE =====
    void TryAttack()
    {
        if (Time.time < nextAttackTime)
            return;

        if (!CheckVision())
            return;

        Vector3 eyePos = transform.position + Vector3.up * data.eyeHeight;
        Vector3 toPlayer = (player.position + Vector3.up) - eyePos;

        if (Physics.Raycast(eyePos, toPlayer.normalized,
            out RaycastHit hit, data.viewDistance, data.obstructionMask))
        {
            if (hit.collider.transform != player)
                return;
        }

        nextAttackTime = Time.time + attackRate;

        if (playerStats != null)
        {
            playerStats.TakeDamage((int)attackDamage);
            Debug.Log("🔫 SOLDIER disparó y dañó al jugador: " + attackDamage);
        }
    }

    // ===== PATRULLA =====
    void PatrolLogic()
    {
        if (patrolPath == null || patrolPath.points.Length == 0)
            return;

        Transform target = patrolPath.points[patrolIndex];
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0;

        Vector3 dir = toTarget.normalized;

        rb.linearVelocity = new Vector3(
            dir.x * patrolSpeed,
            rb.linearVelocity.y,
            dir.z * patrolSpeed
        );

        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        if (toTarget.magnitude < patrolReachDistance)
            patrolIndex = (patrolIndex + 1) % patrolPath.points.Length;
    }

    // ===== MISC =====
    void SetState(State s)
    {
        state = s;
        UpdateStateUI();
    }

    void UpdateStateUI()
    {
        if (stateText != null)
            stateText.text = state.ToString().ToLower();
    }

    IEnumerator HideStateTextAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        if (stateText != null) stateText.gameObject.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        if (data == null) return;

        Gizmos.color = Color.yellow;
        Vector3 eye = transform.position + Vector3.up * data.eyeHeight;

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
}
