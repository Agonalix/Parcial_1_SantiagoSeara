using UnityEngine;
using System.Collections.Generic;

public static class EnemyGlobalAlertSystem
{
    static List<Enemy> allEnemies = new List<Enemy>();

    public static void Register(Enemy e)
    {
        if (!allEnemies.Contains(e))
            allEnemies.Add(e);
    }

    public static void Unregister(Enemy e)
    {
        if (allEnemies.Contains(e))
            allEnemies.Remove(e);
    }

    // Llamado por cámaras o enemigos que detectan al jugador
    public static void AlertAllEnemies()
    {
        foreach (var e in allEnemies)
        {
            e.ForceAlert();
        }
    }
}
