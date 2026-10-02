using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 5f;
    public float gravedad = -9.81f;

    private bool automaticMode;
    private Vector2 automaticInput;

    private CharacterController controller;
    private Animator animator;

    private float velocidadVertical;

    // Control de RollBackward
    private bool rollingBackward;
    private float rollBackwardTimer;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Elegir input automático o teclado
        Vector2 input = automaticMode
            ? automaticInput
            : Keyboard.current != null
                ? new Vector2(
                    (Keyboard.current.dKey.isPressed ? 1 : 0) -
                    (Keyboard.current.aKey.isPressed ? 1 : 0),

                    (Keyboard.current.wKey.isPressed ? 1 : 0) -
                    (Keyboard.current.sKey.isPressed ? 1 : 0)
                )
                : Vector2.zero;

        Vector3 movimiento = new Vector3(input.x, 0f, input.y);

        if (movimiento.magnitude > 1f)
        {
            movimiento.Normalize();
        }

        // Controlar duración del RollBackward
        if (rollingBackward)
        {
            rollBackwardTimer -= Time.deltaTime;

            if (rollBackwardTimer <= 0f)
            {
                rollingBackward = false;
            }
        }

        // Animación de movimiento normal
        if (animator != null && !rollingBackward)
        {
            float speedAnim =
                automaticMode && input.y < 0f
                ? 0f
                : movimiento.magnitude;

            animator.SetFloat("Speed", speedAnim);
        }

        // Gravedad
        if (controller.isGrounded && velocidadVertical < 0f)
        {
            velocidadVertical = -2f;
        }

        velocidadVertical += gravedad * Time.deltaTime;

        Vector3 movimientoFinal = movimiento * velocidad;
        movimientoFinal.y = velocidadVertical;

        controller.Move(movimientoFinal * Time.deltaTime);

        // Girar hacia donde se mueve
        // No gira durante retroceso automático
        if (movimiento != Vector3.zero &&
            !(automaticMode && input.y < 0f))
        {
            transform.forward = movimiento;
        }
    }

    public void EnableAutomaticMode()
    {
        automaticMode = true;
        automaticInput = Vector2.zero;
    }

    public void SetAutomaticInput(Vector2 input)
    {
        automaticInput = Vector2.ClampMagnitude(input, 1f);
    }


    public void Teleport(Vector3 position)
    {
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        bool wasEnabled = controller.enabled;

        controller.enabled = false;
        transform.position = position;
        controller.enabled = wasEnabled;

        velocidadVertical = 0f;
        automaticInput = Vector2.zero;
    }

    public void PlayRollBackward(float duration = 0.8f)
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
            return;

        rollingBackward = true;
        rollBackwardTimer = duration;

        animator.SetFloat("Speed", 0f);
        animator.ResetTrigger("RollBackward");
        animator.SetTrigger("RollBackward");
    }
    public void PlayJump()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger("Jump");
        }
    }

    public void PlayBuff()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger("Buff");
        }
    }

    public void PlayDeath()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger("Death");
        }
    }
}