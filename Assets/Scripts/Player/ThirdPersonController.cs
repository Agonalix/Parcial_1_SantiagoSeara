using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f;
    public float gravity = -9.81f;

    [Header("Refs")]
    public Transform cameraPivot;

    [Header("Crouch")]
    [Tooltip("Porcentaje de velocidad al agacharse (0.75 = 75%)")]
    public float crouchSpeedMultiplier = 0.75f;
    [Tooltip("Multiplicador de escala/altura al agacharse (0.5 = mitad)")]
    public float crouchScaleMultiplier = 0.5f;

    CharacterController cc;
    Vector3 velocity;

    bool isCrouching;

    float originalMoveSpeed;
    float originalHeight;
    Vector3 originalCenter;
    Vector3 originalScale;

    void Awake()
    {
        cc = GetComponent<CharacterController>();

        originalMoveSpeed = moveSpeed;
        originalHeight = cc.height;
        originalCenter = cc.center;
        originalScale = transform.localScale;
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v).normalized;

        bool crouchKey =
            Input.GetKey(KeyCode.C) ||
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);

        SetCrouch(crouchKey);

        Vector3 moveDir = Vector3.zero;
        if (cameraPivot != null)
        {
            Vector3 camFwd = cameraPivot.forward; camFwd.y = 0f; camFwd.Normalize();
            Vector3 camRight = cameraPivot.right; camRight.y = 0f; camRight.Normalize();
            moveDir = (camFwd * input.z + camRight * input.x).normalized;
        }
        else moveDir = input;

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }

        cc.Move(moveDir * moveSpeed * Time.deltaTime);

        if (cc.isGrounded && velocity.y < 0f) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }

    void SetCrouch(bool crouch)
    {
        if (crouch == isCrouching) return;

        isCrouching = crouch;

        if (isCrouching)
        {
            // 1) Escala visual ↓
            transform.localScale = new Vector3(
                originalScale.x,
                originalScale.y * crouchScaleMultiplier,
                originalScale.z
            );

            // 2) Velocidad ↓
            moveSpeed = originalMoveSpeed * crouchSpeedMultiplier;

            // 3) Collider ↓
            cc.height = originalHeight * crouchScaleMultiplier;

            // Ajuste del center para que no flote
            cc.center = new Vector3(
                originalCenter.x,
                originalCenter.y * crouchScaleMultiplier,
                originalCenter.z
            );
        }
        else
        {
            // restaurar escala
            transform.localScale = originalScale;

            moveSpeed = originalMoveSpeed;
            cc.height = originalHeight;
            cc.center = originalCenter;
        }
    }
}