using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [Header("Disparo (semiautomático)")]
    public float damage = 20f;
    public float range = 25f;
    public float fireRate = 2f;

    [Header("Cargador")]
    public int magSize = 15;
    public int bulletsInMag = 15;

    // --- NUEVO: Cargadores limitados ---
    public int remainingMags = 2;   // la consigna pide 2 cargadores extras

    public KeyCode reloadKey = KeyCode.R;

    [Header("Masks")]
    public LayerMask hittableLayers;

    [Header("Refs")]
    public Camera cam;

    float nextAllowed;

    void Reset()
    {
        fireRate = 2f;
        range = 25f;
        damage = 20f;

        magSize = 15;
        bulletsInMag = magSize;

        remainingMags = 2; // reset por si usás Reset()
    }

    void Update()
    {
        if (cam == null) return;

        // Recargar
        if (Input.GetKeyDown(reloadKey))
        {
            TryReload();
        }

        // Disparo semiautomático
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAllowed)
        {
            if (bulletsInMag <= 0)
            {
                Debug.Log("Sin balas. Recargar con R.");
                return;
            }

            nextAllowed = Time.time + (1f / Mathf.Max(0.01f, fireRate));

            Shoot();
            bulletsInMag--;
        }
    }

    void TryReload()
    {
        // Si ya está lleno → NO recarga
        if (bulletsInMag >= magSize)
        {
            Debug.Log("Cargador lleno, no puedo recargar.");
            return;
        }

        // Si no quedan cargadores → NO recarga
        if (remainingMags <= 0)
        {
            Debug.Log("No tengo cargadores restantes.");
            return;
        }

        // Usar un cargador
        remainingMags--;

        // Rellenar el cargador
        bulletsInMag = magSize;

        Debug.Log($"Recargado. Cargadores restantes: {remainingMags}");
    }

    void Shoot()
    {
        Ray viewRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        int mask = hittableLayers;
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0) mask &= ~(1 << playerLayer);

        Vector3 targetPoint = viewRay.origin + viewRay.direction * range;
        if (Physics.Raycast(viewRay, out RaycastHit sightHit, range, mask, QueryTriggerInteraction.Collide))
            targetPoint = sightHit.point;

        Vector3 shootDir = (targetPoint - cam.transform.position).normalized;

        if (Physics.Raycast(cam.transform.position, shootDir, out RaycastHit hit, range, mask, QueryTriggerInteraction.Collide))
        {
            // Soldier
            if (hit.collider.TryGetComponent<Enemy>(out var e) ||
               (e = hit.collider.GetComponentInParent<Enemy>()) != null)
            {
                e.TakeDamage(damage);
                Debug.Log("✔ Soldier recibió daño");
                return;
            }

            // Cámara
            if (hit.collider.TryGetComponent<SurveillanceCamera>(out var camEnemy) ||
               (camEnemy = hit.collider.GetComponentInParent<SurveillanceCamera>()) != null)
            {
                camEnemy.TakeDamage(damage);
                Debug.Log("📹 Cámara recibió daño");
                return;
            }

            Debug.Log($"Ray hit {hit.collider.name}");
        }
    }
    public void ResetAmmo()
    {
        // Resetea el cargador actual
        bulletsInMag = magSize;

        // Resetea los cargadores restantes a 2 (consigna del parcial)
        remainingMags = 2;
    }
}
