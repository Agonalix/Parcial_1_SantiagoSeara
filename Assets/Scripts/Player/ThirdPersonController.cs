using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f;
    public float gravity = -9.81f;

    [Header("Refs")]
    public Transform cameraPivot; // arrastrá el CameraPivot del Player

    [Header("Crouch")]
    [Tooltip("Porcentaje de velocidad al agacharse (0.75 = 75%)")]
    public float crouchSpeedMultiplier = 0.75f;
    [Tooltip("Multiplicador de altura del CharacterController al agacharse (0.5 = mitad)")]
    public float crouchHeightMultiplier = 0.5f;

    CharacterController cc;
    Vector3 velocity;

    bool isCrouching;
    float originalMoveSpeed;
    float originalHeight;
    Vector3 originalCenter;

    void Awake()
    {
        cc = GetComponent<CharacterController>();

        // Guardamos valores originales
        originalMoveSpeed = moveSpeed;
        originalHeight = cc.height;
        originalCenter = cc.center;
    }

    void Update()
    {
        // --- INPUT DE MOVIMIENTO ---
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v).normalized;

        // --- INPUT DE AGACHARSE (C o Ctrl) ---
        bool crouchKey =
            Input.GetKey(KeyCode.C) ||
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);

        SetCrouch(crouchKey);

        // --- DIRECCIÓN SEGÚN CÁMARA ---
        Vector3 moveDir = Vector3.zero;
        if (cameraPivot != null)
        {
            Vector3 camFwd = cameraPivot.forward; camFwd.y = 0f; camFwd.Normalize();
            Vector3 camRight = cameraPivot.right; camRight.y = 0f; camRight.Normalize();
            moveDir = (camFwd * input.z + camRight * input.x).normalized;
        }
        else
        {
            moveDir = input;
        }

        // --- ROTACIÓN DEL PLAYER ---
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }

        // --- MOVIMIENTO HORIZONTAL ---
        cc.Move(moveDir * moveSpeed * Time.deltaTime);

        // --- GRAVEDAD ---
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
            // Velocidad reducida 25% → queda al 75%
            moveSpeed = originalMoveSpeed * crouchSpeedMultiplier;

            // Altura del collider a la mitad
            cc.height = originalHeight * crouchHeightMultiplier;

            // Ajustamos el centro para que no se hunda tanto
            cc.center = new Vector3(
                originalCenter.x,
                originalCenter.y * crouchHeightMultiplier,
                originalCenter.z
            );
        }
        else
        {
            moveSpeed = originalMoveSpeed;
            cc.height = originalHeight;
            cc.center = originalCenter;
        }
    }
}