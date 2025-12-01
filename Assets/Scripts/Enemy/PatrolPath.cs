using UnityEngine;

public class PatrolPath : MonoBehaviour
{
    public Transform[] points;

    void OnDrawGizmos()
    {
        if (points == null || points.Length == 0) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 a = points[i].position;
            Vector3 b = points[(i + 1) % points.Length].position;
            Gizmos.DrawSphere(a, 0.2f);
            Gizmos.DrawLine(a, b);
        }
    }
}
