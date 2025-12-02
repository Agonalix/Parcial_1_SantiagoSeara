using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData data;   // arrastrás el SO acá

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerStats stats = other.GetComponent<PlayerStats>();
        PlayerShoot shoot = other.GetComponent<PlayerShoot>();

        if (stats == null || shoot == null)
        {
            Debug.LogWarning("Falta PlayerStats o PlayerShoot en el Player.");
            return;
        }

        switch (data.type)
        {
            case ItemType.Medikit:
                stats.AddHealth(data.healAmount);
                Debug.Log($"❤️ Medikit → curado {data.healAmount}");
                break;

            case ItemType.Magazine:
                shoot.remainingMags += data.magsToAdd;
                Debug.Log($"🔫 Magazine → +{data.magsToAdd} cargadores");
                break;
        }

        Destroy(gameObject);
    }
}
