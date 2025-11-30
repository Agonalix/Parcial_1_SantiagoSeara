using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Enemy : MonoBehaviour
{
    public enum State { Patrol, Normal, Chase, Alert, Damage, Dead }

    [Header("Config")]
    public Soldier data;

    [Header("Estado actual")]
    public State state = State.Patrol;

    [Header("Referencias")]
    public Transform player;
    PlayerStats playerStats;

    [Header("UI Estado")]
    public TextMeshPro stateText;
    public float textHeight = 2f;

    [Header("Patrulla")]
    public Transform[] patrolPoints;
    int patrolIndex = 0;
    public float patrolWaitTime = 1f;
    float patrolWaitTimer;

    [Header("Ataque")]
    public float attackRange = 1.5f;
    public int damagePerHit = 10;
    public float attackCooldown = 1f;
    float attackTimer;

    Rigidbody rb;
    float health;
    bool hasDetectedOnce = false;

    // ALERTA GLOBAL
    public static bool globalAlert = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            Debug.LogError("Enemy necesita Rigidbody");

        rb.freezeRotation = true;
    }

    void Start()
    {
        if (data == null)
        {
            Debug.LogError("No asignaste Soldier");
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

        if (globalAlert && state != State.Chase && state != State.Alert)
        {
            SetState(State.Alert);
            return;
        }

        if (state == State.Patrol)
            PatrolLogic();

        bool seesPlayer = CheckVision();

        if (seesPlayer)
        {
            hasDetectedOnce = true;
            globalAlert = true;
            SetState(State.Chase);
        }
    }

    void FixedUpdate()
    {
        if (state == State.Chase || state == State.Alert)
            ChasePlayerMovement();
    }

    // ---------- PATRULLA ----------
    void PatrolLogic()
    {
        if (patrolPoints.Length == 0)
        {
            SetState(State.Normal);
            return;
        }

        Transform target = patrolPoints[patrolIndex];
        Vector3 dir = (target.position - transform.position);
        dir.y = 0;

        if (dir.magnitude < 0.2f)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitTime)
            {
                patrolWaitTimer = 0;
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            }
            return;
        }

        Vector3 move = dir.normalized * data.moveSpeed;
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
        transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    // ---------- CHASE / ALERT ----------
    void ChasePlayerMovement()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - transform.position;
        float dist = toPlayer.magnitude;

        toPlayer.y = 0;
        Vector3 dir = toPlayer.normalized;

        rb.linearVelocity = new Vector3(dir.x * data.moveSpeed, rb.linearVelocity.y, dir.z * data.moveSpeed);

        transform.rotation = Quaternion.LookRotation(dir);

        attackTimer += Time.deltaTime;

        if (dist < attackRange)
        {
            TryAttackPlayer();
        }
    }

    void TryAttackPlayer()
    {
        if (attackTimer < attackCooldown) return;

        attackTimer = 0;

        if (playerStats != null)
        {
            playerStats.TakeDamage(damagePerHit);
            Debug.Log("Jugador recibe daño: " + damagePerHit);
        }
    }

    // ---------- DETECCIÓN ----------
    bool CheckVision()
    {
        if (player == null) return false;

        Vector3 eyePos = transform.position + Vector3.up * data.eyeHeight;
        Vector3 toPlayer = (player.position + Vector3.up * 1f) - eyePos;

        float dist = toPlayer.magnitude;
        if (dist > data.viewDistance) return false;

        Vector3 flatDir = toPlayer;
        flatDir.y = 0f;

        float angle = Vector3.Angle(transform.forward, flatDir);
        if (angle > data.viewAngle) return false;

        if (Physics.Raycast(eyePos, toPlayer.normalized, out RaycastHit hit, data.viewDistance, data.obstructionMask))
        {
            if (hit.collider.transform != player)
                return false;
        }

        return true;
    }

    // ---------- DAÑO ----------
    public void TakeDamage(float dmg)
    {
        if (state == State.Dead) return;

        health -= dmg;
        if (health <= 0)
        {
            Die();
            return;
        }

        if (!hasDetectedOnce)
            StartCoroutine(AlertAfter3Sec()); // si no lo matan, entra en alerta

        SetState(State.Damage);
        StopAllCoroutines();
        StartCoroutine(BackToChase());
    }

    IEnumerator AlertAfter3Sec()
    {
        yield return new WaitForSeconds(3f);
        if (state != State.Dead)
        {
            globalAlert = true;
            SetState(State.Alert);
        }
    }

    IEnumerator BackToChase()
    {
        yield return new WaitForSeconds(0.15f);
        if (globalAlert) SetState(State.Alert);
        else if (hasDetectedOnce) SetState(State.Chase);
        else SetState(State.Patrol);
    }

    void Die()
    {
        state = State.Dead;
        UpdateStateUI();

        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        StartCoroutine(HideStateText());
    }

    IEnumerator HideStateText()
    {
        yield return new WaitForSeconds(3f);
        if (stateText != null) stateText.gameObject.SetActive(false);
    }

    // ---------- UTILS ----------
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

    void OnDrawGizmosSelected()
    {
        if (data == null) return;

        Gizmos.color = Color.yellow;

        Vector3 eye = transform.position + Vector3.up * data.eyeHeight;

        Vector3 left = Quaternion.Euler(0, -data.viewAngle, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, data.viewAngle, 0) * transform.forward;

        Gizmos.DrawRay(eye, left * data.viewDistance);
        Gizmos.DrawRay(eye, right * data.viewDistance);
        Gizmos.DrawRay(eye, transform.forward * data.viewDistance);
    }
}
