using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum State { Normal, Chase, Damage, Dead }

    [Header("Vida")]
    public float maxHealth = 50f;
    public float health = 50f;

    [Header("Persecución por distancia (nota 4)")]
    public float chaseDistance = 5f;

    [Header("Cono de visión (7–10)")]
    public bool useVisionCone = false;
    [Tooltip("Mitad del ángulo del cono (60 => total 120°)")]
    public float viewAngle = 60f;
    public float viewDistance = 8f;

    [Header("Movimiento")]
    public float moveSpeed = 3f;

    [Header("Drain de Estamina")]
    public int staminaDrainPerSec = 1;
    public float drainInterval = 1f;

    [Header("Referencias")]
    public Transform player;

    // Internos
    PlayerStats playerStats;
    Vector3 spawnPos;
    Quaternion spawnRot;
    float drainTimer;
    State state = State.Normal;

    Renderer[] _renderers;
    Collider[] _colliders;

    // Solo un enemigo activo
    static Enemy activeEnemy;

    void Awake()
    {
        spawnPos = transform.position;
        spawnRot = transform.rotation;

        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);

        if (activeEnemy != null && activeEnemy != this)
        {
            Debug.LogWarning("Ya existe un enemigo activo. Desactivando este.");
            gameObject.SetActive(false);
            return;
        }
        activeEnemy = this;
    }

    void Start()
    {
        if (player == null)
        {
            var pGO = GameObject.FindGameObjectWithTag("Player");
            if (pGO != null) player = pGO.transform;
        }
        if (player != null) playerStats = player.GetComponent<PlayerStats>();

        health = Mathf.Clamp(health, 0f, maxHealth);
        LogState();
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        // F3: respawn
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Respawn();
            return;
        }

        if (state == State.Dead) return;

        bool shouldChase = ShouldChasePlayer();

        switch (state)
        {
            case State.Normal:
                if (shouldChase) SetState(State.Chase);
                break;

            case State.Chase:
                if (!shouldChase)
                {
                    SetState(State.Normal);
                    break;
                }

                if (player != null)
                {
                    Vector3 dir = player.position - transform.position;
                    dir.y = 0f;
                    transform.position += dir.normalized * moveSpeed * Time.deltaTime;
                    if (dir.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                }

                HandleStaminaDrain();
                break;

            case State.Damage:
                // vuelve solo en la corrutina
                break;
        }
    }

    bool ShouldChasePlayer()
    {
        if (player == null) return false;

        if (useVisionCone)
        {
            Vector3 toPlayer = player.position - transform.position;
            float dist = toPlayer.magnitude;
            if (dist > viewDistance) return false;

            Vector3 flatToPlayer = toPlayer; flatToPlayer.y = 0f;
            Vector3 forward = transform.forward; forward.y = 0f;
            float angle = Vector3.Angle(forward, flatToPlayer);

            return angle <= viewAngle;
        }
        else
        {
            return Vector3.Distance(transform.position, player.position) <= chaseDistance;
        }
    }

    void HandleStaminaDrain()
    {
        if (playerStats == null) return;

        playerStats.PauseRegen(true);

        drainTimer += Time.deltaTime;
        if (drainTimer >= drainInterval)
        {
            drainTimer = 0f;
            int before = playerStats.stamina;
            playerStats.DrainStamina(staminaDrainPerSec);
            Debug.Log($"[DRAIN] Stamina {before} → {playerStats.stamina}");
        }
    }

    public void TakeDamage(float dmg)
    {
        if (state == State.Dead) return;

        health = Mathf.Max(0f, health - dmg);
        SetState(State.Damage);
        Debug.Log("Enemy state: damage");

        if (health <= 0f)
        {
            Die();
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(BriefDamageState());
        }
    }

    IEnumerator BriefDamageState()
    {
        yield return new WaitForSeconds(0.1f);
        state = ShouldChasePlayer() ? State.Chase : State.Normal;
        LogState();
    }

    void Die()
    {
        state = State.Dead;
        LogState();
        SetVisibleAndCollidable(false);
        if (playerStats != null) playerStats.PauseRegen(false);
    }

    void Respawn()
    {
        SetVisibleAndCollidable(true);
        transform.position = spawnPos;
        transform.rotation = spawnRot;
        health = maxHealth;
        drainTimer = 0f;
        state = State.Normal;
        LogState();
    }

    void SetVisibleAndCollidable(bool enable)
    {
        foreach (var r in _renderers) if (r != null) r.enabled = enable;
        foreach (var c in _colliders) if (c != null) c.enabled = enable;
    }

    void SetState(State s)
    {
        if (state == s) return;
        if (state == State.Chase && s != State.Chase)
            playerStats?.PauseRegen(false);

        state = s;
        LogState();

        if (state == State.Chase)
            drainTimer = 0f;
    }

    void LogState()
    {
        Debug.Log($"Enemy state: {state.ToString().ToLower()}");
    }

    void OnDrawGizmosSelected()
    {
        // Dibuja el cono de visión para ayudar al profe a verlo
        if (!useVisionCone) return;

        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + Vector3.up * 1f;
        Gizmos.DrawLine(origin, origin + transform.forward * viewDistance);

        int steps = 20;
        for (int i = -steps; i <= steps; i++)
        {
            float t = (float)i / steps;
            float ang = t * viewAngle;
            Quaternion rot = Quaternion.AngleAxis(ang, Vector3.up);
            Vector3 dir = rot * transform.forward;
            Gizmos.DrawLine(origin, origin + dir.normalized * viewDistance);
        }
    }
}