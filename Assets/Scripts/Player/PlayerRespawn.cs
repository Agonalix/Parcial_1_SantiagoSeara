using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerRespawn : MonoBehaviour
{
    public PlayerStats stats;      // arrastrá PlayerStats desde el Inspector
    public PlayerShoot shoot;      // arrastrá PlayerShoot
    public Transform spawnPoint;   // empty con la posición de respawn

    CharacterController cc;

    void Start()
    {
        cc = GetComponent<CharacterController>();

        // Si no asignaste spawnPoint, usa la posición inicial del jugador
        if (spawnPoint == null)
        {
            GameObject sp = new GameObject("AutoSpawnPoint");
            sp.transform.position = transform.position;
            spawnPoint = sp.transform;
        }
    }

    void Update()
    {
        // --------- REAPARECER (F1) ---------
        if (Input.GetKeyDown(KeyCode.F1))
        {
            RespawnPlayer();
        }

        // --------- REINICIAR ESCENA COMPLETA (F2) ---------
        if (Input.GetKeyDown(KeyCode.F2))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    void RespawnPlayer()
    {
        // 1) Resetear vida del jugador
        stats.health = stats.maxHealth;

        // 2) Resetear munición (balas + cargadores)
        if (shoot != null)
            shoot.ResetAmmo();

        // 3) Resetear posición de manera SEGURA
        cc.enabled = false;
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;
        cc.enabled = true;

        // 4) Resetear velocidad por si acaso
        if (TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log("🔄 Jugador reaparecido con munición y vida restauradas");
    }
}
