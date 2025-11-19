using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("Refs")]
    public PlayerStats playerStats;
    public PlayerShoot playerShoot;

    [Header("Vida")]
    public Slider healthBar;
    public TextMeshProUGUI healthText;

    [Header("Balas")]
    public TextMeshProUGUI ammoText;

    void Start()
    {
        if (playerStats == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerStats = p.GetComponent<PlayerStats>();
        }

        if (playerShoot == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerShoot = p.GetComponent<PlayerShoot>();
        }

        if (healthBar != null)
            healthBar.maxValue = playerStats.maxHealth;
    }

    void Update()
    {
        if (playerStats != null)
        {
            if (healthBar != null)
                healthBar.value = playerStats.health;

            if (healthText != null)
                healthText.text = playerStats.health.ToString();
        }

        if (playerShoot != null && ammoText != null)
        {
            ammoText.text = $"{playerShoot.bulletsInMag}/{playerShoot.magSize}";
        }
    }
}