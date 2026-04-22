using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ThirdPersonController : MonoBehaviour
{
    [FoldoutGroup("References")]
    public InputSystem_Actions inputs;
    [FoldoutGroup("References")]
    private CharacterController controller;
    [FoldoutGroup("References")]
    public CinemachineCamera characterCamera;
    [FoldoutGroup("References")]
    public Animator animator;

    private bool isDead = false;

    [FoldoutGroup("Controller")]
    public float moveSpeed = 5f;
    [FoldoutGroup("Controller")]
    public float rotationSpeed = 200f;
    [FoldoutGroup("Controller")]
    public float verticalVelocity = 0;
    [FoldoutGroup("Controller")]
    public float jumpForce = 10;
    [FoldoutGroup("Controller")]
    public float pushForce = 4;

    [FoldoutGroup("Controller/Dash")]
    private bool IsDashing;
    [FoldoutGroup("Controller/Dash")]
    public float dashForce;
    [FoldoutGroup("Controller/Dash")]
    public float dashDuration = 0.2f;
    [FoldoutGroup("Controller/Dash")]
    private float dashTimer;
    [FoldoutGroup("Controller/Animator"), SerializeField]
    private CinemachineImpulseSource source;

    [SerializeField] private Vector2 moveInput;

    [SerializeField] private CinemachineImpulseSource damageSource;

    [FoldoutGroup("WallRun")]
    public float rayLenght;
    [FoldoutGroup("WallRun")]
    public float cameraTitlt = 15;
    [FoldoutGroup("WallRun")]
    public float maxTimeInAir;
    [FoldoutGroup("WallRun")]
    public bool enableWallRun;

    [Header("Vida")]
    public int maxHealth = 100;
    public int currentHealth;

    [SerializeField] private Slider healthSlider;

    [Header("Wall Jump")]
    public float wallJumpForce = 8f;
    public float wallJumpUpForce = 10f;
    public float wallJumpCooldown = 1f;

    private bool canWallJump = true;

    Vector3 normalDebug;
    Vector3 impactPoint;
    Vector3 crossResult;
    Vector3 wallJumpVelocity;

    private void Awake()
    {
        inputs = new();
        controller = GetComponent<CharacterController>();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    private void OnEnable()
    {
        inputs.Enable();

        inputs.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputs.Player.Move.canceled += ctx => moveInput = Vector2.zero;


        inputs.Player.Jump.performed += OnJump;

        inputs.Player.Sprint.performed += OnDash;
    }
    void Start()
    {
        currentHealth = maxHealth;
    }
    void Update()
    {
        if (isDead) return;
        EnableWallRun();
        OnMove();
        //OnSimpleMove();
    }

    public void OnMove()
    {
        Vector3 cameraForwardDir = characterCamera.transform.forward;
        cameraForwardDir.y = 0;
        cameraForwardDir.Normalize();


        if (moveInput != Vector2.zero)
        {
            Quaternion targetQuaternion = Quaternion.LookRotation(cameraForwardDir);
            //transform.rotation = targetQuaternion;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetQuaternion,
                rotationSpeed * Time.deltaTime);



        }
        //>?
        Vector3 moveDir;
        if (!enableWallRun)
        {
            moveDir = (cameraForwardDir * moveInput.y + transform.right * moveInput.x) * moveSpeed;
        }
        else
        {
            moveDir = (crossResult * moveInput.y) * moveSpeed;



        }

        float magnitud = Mathf.Abs(controller.velocity.magnitude);
        // print(magnitud);
        animator.SetFloat("Speed", magnitud);


        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        if (enableWallRun && canWallJump)
            verticalVelocity = 0;

        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;


        moveDir.y = verticalVelocity;
        moveDir += wallJumpVelocity;

        animator.SetBool("Grounded", controller.isGrounded);


        if (IsDashing)
        {
            //->convertir el dash a un barrido por el piso! dash con gravedad integrada omaegoto!
            moveDir = transform.forward * dashForce * (dashTimer / dashDuration);

            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0)
                IsDashing = false;
        }
        controller.Move(moveDir * Time.deltaTime);
        wallJumpVelocity = Vector3.Lerp(wallJumpVelocity, Vector3.zero, Time.deltaTime * 5f);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (enableWallRun && canWallJump)
        {
            WallJump();
            return;
        }
   
        if (!controller.isGrounded) return;

        animator.SetTrigger("Jump");
        source.GenerateImpulse();
        verticalVelocity = jumpForce;
    }
    public void OnSimpleMove()
    {
        transform.Rotate(Vector3.up * moveInput.x * rotationSpeed * Time.deltaTime);
        Vector3 moveDir = transform.forward * moveSpeed * moveInput.y;
        controller.SimpleMove(moveDir);
    }
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {


        Vector3 pushDir = (hit.transform.position - transform.position).normalized;

        if (hit.rigidbody != null && hit.rigidbody.linearVelocity == Vector3.zero)
        {
            print(hit.gameObject.name);
            hit.rigidbody.AddForce(pushDir * pushForce, ForceMode.Impulse);
        }
    }
    private void OnDash(InputAction.CallbackContext context)
    {
        IsDashing = true;
        dashTimer = dashDuration;
    }

    public void EnableWallRun()
    {
        //->mejor castearlo desde una referenia en los piez
        RaycastHit hit = default;

        Physics.Raycast(transform.position, transform.right, out RaycastHit hitRight, rayLenght);

        Physics.Raycast(transform.position, -transform.right, out RaycastHit hitLeft, rayLenght);


        if (hitRight.collider != null && hitRight.collider.gameObject.tag == "Wall")
        {
            hit = hitRight;
            characterCamera.Lens.Dutch = cameraTitlt;
        }
        else if (hitLeft.collider != null && hitLeft.collider.gameObject.tag == "Wall")
        {
            hit = hitLeft;
            characterCamera.Lens.Dutch = -cameraTitlt;
        }
        else
        {
            characterCamera.Lens.Dutch = 0;
            enableWallRun = false;
        }

        if (hit.collider != null)
        {
            enableWallRun = true;
            Debug.Log("AleluyaR");

            normalDebug = hit.normal;
            impactPoint = hit.point;
            crossResult = Vector3.Cross(normalDebug, transform.up);//+1

            if (Vector3.Dot(crossResult, transform.forward) < 0)
            {
                crossResult *= -1;
            }
        }






        /*
        if (hitRight.collider != null &&  hitRight.collider.gameObject.tag == "Wall")
        {



            enableWallRun = true;
            Debug.Log("AleluyaR");

            normalDebug = hitRight.normal;
            impactPoint = hitRight.point;
            crossResult = Vector3.Cross(normalDebug, transform.up);//+1

            if( Vector3.Dot(crossResult,transform.forward) < 0)
            {
                crossResult *= -1;
            }


        }
        else
        {
            enableWallRun =false;
        }

        if (hitLeft.collider != null && hitLeft.collider.gameObject.tag == "Wall")
        {
            Debug.Log("AleluyaL");
        }*/
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.purple;
        Gizmos.DrawRay(transform.position, transform.right * rayLenght);
        Gizmos.color = Color.navyBlue;
        Gizmos.DrawRay(transform.position, -transform.right * rayLenght);

        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(impactPoint, normalDebug * rayLenght);
        Gizmos.DrawSphere(impactPoint, 0.1f);

        Gizmos.color = Color.orange;
        Gizmos.DrawRay(impactPoint, crossResult * rayLenght);


    }
    public void TakeDamage(Vector3 hitDirection)
    {
        if (isDead) return;
        currentHealth -= 10;
        damageSource.GenerateImpulse(-hitDirection);

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();

        }


    }
    void UpdateHealthUI()
    {
        healthSlider.value = currentHealth;
    }
    void Die()
    {
        isDead = true;
        Debug.Log("Jugador muerto");
        controller.enabled = false;
        animator.SetBool("Grounded", false);
        Invoke(nameof(RestartScene), 2f);
    }
    void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
  
    void WallJump()
    {
        canWallJump = false;
        Vector3 jumpDir = normalDebug + Vector3.up;
        wallJumpVelocity = jumpDir.normalized * wallJumpForce;
        verticalVelocity = wallJumpUpForce;
        source.GenerateImpulse();

        StartCoroutine(WallJumpCooldown());
    }
    IEnumerator WallJumpCooldown()
    {
        yield return new WaitForSeconds(wallJumpCooldown);
        canWallJump = true;
    }
}



