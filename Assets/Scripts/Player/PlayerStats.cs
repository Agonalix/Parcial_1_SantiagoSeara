using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Datos base (SO) — opcional")]
    public PlayerStatsData data; // Si lo asignás, se usan estos valores

    [Header("Vida (runtime)")]
    public int maxHealth = 100;
    public int health = 100;

    [Header("Estamina (runtime)")]
    public int maxStamina = 10;
    public int stamina = 10;

    [Tooltip("segundos por punto de estamina")]
    public float regenInterval = 1f;

    bool pauseRegen;
    float regenTimer;

    // Lee del ScriptableObject (si está asignado) ANTES de Start de otros
    void OnEnable()
    {
        if (data != null)
        {
            maxHealth = Mathf.Max(1, data.maxHealth);
            health = Mathf.Clamp(data.startHealth, 0, maxHealth);

            maxStamina = Mathf.Max(1, data.maxStamina);
            stamina = Mathf.Clamp(data.startStamina, 0, maxStamina);

            regenInterval = Mathf.Max(0.0001f, data.regenInterval);
        }
        else
        {
            // Sin SO asignado, mantené tus defaults actuales
            maxHealth = Mathf.Max(1, maxHealth);
            health = Mathf.Clamp(health, 0, maxHealth);

            maxStamina = Mathf.Max(1, maxStamina);
            stamina = Mathf.Clamp(stamina, 0, maxStamina);

            regenInterval = Mathf.Max(0.0001f, regenInterval);
        }
    }

    void Reset()
    {
        maxHealth = health = 100;
        maxStamina = stamina = 10;
        regenInterval = 1f;
    }

    void Update()
    {
        if (pauseRegen) return;

        regenTimer += Time.deltaTime;
        if (regenTimer >= regenInterval)
        {
            regenTimer = 0f;
            if (stamina < maxStamina) stamina += 1;
        }

        // CHECK DE MUERTE
        if (health <= 0)
        {
            Die();
        }

        // REAPARECER CON F1
        if (Input.GetKeyDown(KeyCode.F1))
        {
            RespawnPlayer();
        }

        // REINICIAR ESCENA CON F2
        if (Input.GetKeyDown(KeyCode.F2))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }
    void Die()
    {
        Debug.Log("☠ Jugador MUERTO");

        // Inmovilizamos al personaje
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
    }

    void RespawnPlayer()
    {
        Debug.Log("↻ Jugador reapareció");

        // Restauramos valores
        health = maxHealth;
        stamina = maxStamina;

        // Reposicionar al jugador (punto 0,0,0 o donde quieras)
        transform.position = Vector3.zero;

        // Reactivamos movement
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = true;
    }

    public void PauseRegen(bool pause)
    {
        pauseRegen = pause;
        if (!pause) regenTimer = 0f;
    }

    public void TakeDamage(int dmg)
    {
        health = Mathf.Max(0, health - Mathf.Max(0, dmg));
        if (health == 0) Debug.Log("Player muerto (no requerido en consigna).");
    }

    public void DrainStamina(int amount)
    {
        stamina = Mathf.Max(0, stamina - Mathf.Max(0, amount));
    }

    // Útiles si luego querés pickups o curaciones
    public void AddHealth(int amount)
    {
        int before = health;
        health = Mathf.Clamp(health + amount, 0, maxHealth);
        Debug.Log($"[HEALTH] {before} → {health}");
    }

    public void AddStamina(int amount)
    {
        int before = stamina;
        stamina = Mathf.Clamp(stamina + amount, 0, maxStamina);
        Debug.Log($"[STAMINA] {before} → {stamina}");
    }
}