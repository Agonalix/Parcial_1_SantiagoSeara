using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [Header("Disparo (semiautomático)")]
    public float damage = 20f;
    public float range = 25f;
    public float fireRate = 2f; // disparos por segundo

    [Header("Masks")]
    public LayerMask hittableLayers; // set en Inspector: Enemy y Environment, excluir Player

    [Header("Refs")]
    public Camera cam; // arrastrá la Main Camera

    float nextAllowed;

    void Reset()
    {
        fireRate = 2f;
        range = 25f;
        damage = 20f;
    }

    void Update()
    {
        if (cam == null) return;

        if (Input.GetMouseButtonDown(0) && Time.time >= nextAllowed)
        {
            nextAllowed = Time.time + (1f / Mathf.Max(0.01f, fireRate));
            Shoot();
        }
    }

    void Shoot()
    {
        // SIEMPRE usa la proyección de cámara (evita forwards invertidos)
        Ray viewRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Excluir Player por seguridad
        int mask = hittableLayers;
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0) mask &= ~(1 << playerLayer);

        // Punto objetivo (lo que ves)
        Vector3 targetPoint = viewRay.origin + viewRay.direction * range;
        if (Physics.Raycast(viewRay, out RaycastHit sightHit, range, mask, QueryTriggerInteraction.Collide))
            targetPoint = sightHit.point;

        // Disparo desde la cámara hacia el punto objetivo
        Vector3 shootOrigin = cam.transform.position;
        Vector3 shootDir = (targetPoint - shootOrigin).normalized;

        if (Physics.Raycast(shootOrigin, shootDir, out RaycastHit hit, range, mask, QueryTriggerInteraction.Collide))
        {
            // Buscar Enemy en el objeto golpeado o en sus padres
            if (hit.collider.TryGetComponent<Enemy>(out var e) || (e = hit.collider.GetComponentInParent<Enemy>()) != null)
            {
                e.TakeDamage(damage);
                Debug.Log("✔ Enemy: damage");
            }
            else
            {
                Debug.Log($"Ray hit {hit.collider.name} (sin Enemy en padres)");
            }
        }
    }
}