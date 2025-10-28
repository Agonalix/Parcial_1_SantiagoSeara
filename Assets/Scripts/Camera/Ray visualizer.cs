using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RayVisualizer : MonoBehaviour
{
    public Camera cam;        // arrastrá la Main Camera acá (o lo asigna solo)
    public float range = 30f;

    LineRenderer lr;

    void Awake()
    {
        if (cam == null) cam = GetComponent<Camera>();
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.widthMultiplier = 0.02f;
        lr.useWorldSpace = true;
        // Material básico para que se vea
        if (lr.material == null) lr.material = new Material(Shader.Find("Sprites/Default"));
    }

    void Update()
    {
        if (cam == null) return;

        Ray r = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        Vector3 end = r.origin + r.direction * range;

        // si choca algo, cortamos la línea en el hit
        if (Physics.Raycast(r, out var hit, range, ~0, QueryTriggerInteraction.Collide))
            end = hit.point;

        lr.SetPosition(0, r.origin);
        lr.SetPosition(1, end);
    }
}