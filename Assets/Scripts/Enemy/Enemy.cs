using System.Collections;
using UnityEngine;
using TMPro;

public class Enemy : MonoBehaviour
{
    public enum State { Normal, Patrol, Chase, Damage, Dead }

    [Header("Config (ScriptableObject)")]
    public Soldier data;

    [Header("Estado actual")]
    public State state = State.Normal;

    [Header("Referencias")]
    public Transform player;
    PlayerStats playerStats;

    [Header("UI Estado")]
    public TextMeshPro stateText;
    public float textHeight = 2f;

    [Header("Ataque")]
    public float attackDamage = 10f;
    public float attackRate = 1.2f;  // 1 golpe cada 1.2 segundos
    public float attackRange = 1.5f; // distancia a la que puede golpear
    float nextAttackTime;

    float health;
    bool hasDetectedPlayer = false;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        EnemyGlobalAlertSystem.Register(this);  // ← Registro global
    }

    void OnDestroy()
    {
        EnemyGlobalAlertSystem.Unregister(this); // ← Limpieza global
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
            FaceTextToCamera();
            return;
        }

        FaceTextToCamera();

        // Evita IA mientras está recibiendo daño
        if (state == State.Damage)
            return;

        // Si ya está en alerta global o local, siempre persigue
        if (hasDetectedPlayer)
        {
            if (state != State.Chase)
                SetState(State.Chase);

            return;
        }

        // Si está patrullando o normal
        bool seesPlayer = CheckVision();

        if (seesPlayer)
        {
            hasDetectedPlayer = true;
            SetState(State.Chase);

            // ALERTA GLOBAL (si ves al jugador vos)
            EnemyGlobalAlertSystem.AlertAllEnemies();
        }

        // Lógica de patrulla (la completás vos)
        if (state == State.Patrol)
        {
            PatrolLogic();  // ← tu lógica acá
        }
    }

    void FixedUpdate()
    {
        if (state == State.Chase && player != null && state != State.Dead)
        {
            // 1) Intenta atacar si está cerca
            TryAttack();

            // 2) Se mueve hacia el jugador
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

    // --- MOVIMIENTO DE PERSECUCIÓN ---
    void ChasePlayer()
    {
        if (rb == null || player == null) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        Vector3 dir = toPlayer.normalized;

        rb.linearVelocity = new Vector3(
            dir.x * data.moveSpeed,
            rb.linearVelocity.y,
            dir.z * data.moveSpeed
        );

        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    // --- VISIÓN ---
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

        if (Physics.Raycast(eyePos, toPlayer.normalized, out RaycastHit hit, data.viewDistance, data.obstructionMask))
        {
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

        // EN ALERTA SI TE DISPARAN
        EnemyGlobalAlertSystem.AlertAllEnemies();

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
        yield return new WaitForSeconds(0.25f);

        if (hasDetectedPlayer) SetState(State.Chase);
        else SetState(State.Patrol);
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

        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        StartCoroutine(HideStateTextAfterDelay());
    }

    // --- ESTADOS ---
    void SetState(State s)
    {
        state = s;
        UpdateStateUI();
    }

    public void ForceAlert()
    {
        if (state == State.Dead) return;

        hasDetectedPlayer = true;
        SetState(State.Chase);
    }

    void UpdateStateUI()
    {
        if (stateText != null)
            stateText.text = state.ToString().ToLower();
    }

    // --- PATRULLA (vos completás aquí tu recorrido actual) ---
    void PatrolLogic()
    {
        // ACA PEGÁS tu lógica actual de patrulla
        // Sin cambiar nombres ni nada
    }

    // --- DEBUG ---
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

    IEnumerator HideStateTextAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        if (stateText != null) stateText.gameObject.SetActive(false);
    }
    void TryAttack()
    {
        if (Time.time < nextAttackTime)
            return;

        // 1) chequeo si realmente puede ver al jugador (misma lógica del cono)
        if (!CheckVision())
            return;

        // 2) chequear si no hay paredes en el medio
        Vector3 eyePos = transform.position + Vector3.up * data.eyeHeight;
        Vector3 toPlayer = (player.position + Vector3.up * 1f) - eyePos;

        if (Physics.Raycast(eyePos, toPlayer.normalized, out RaycastHit hit, data.viewDistance, data.obstructionMask))
        {
            if (hit.collider.transform != player)
                return; // una pared lo tapa → no dispara
        }

        // 3) si llega acá → DISPARA
        nextAttackTime = Time.time + attackRate;

        if (playerStats != null)
        {
            playerStats.TakeDamage((int)attackDamage);
            Debug.Log("🔫 SOLDIER disparó y dañó al jugador: " + attackDamage);
        }
    }

}