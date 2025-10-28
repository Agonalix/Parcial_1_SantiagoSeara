using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f;
    public float gravity = -9.81f;

    [Header("Refs")]
    public Transform cameraPivot; // arrastrá el CameraPivot del Player

    CharacterController cc;
    Vector3 velocity;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v).normalized;

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

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        cc.Move(moveDir * moveSpeed * Time.deltaTime);

        // gravedad simple
        if (cc.isGrounded && velocity.y < 0f) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }
}