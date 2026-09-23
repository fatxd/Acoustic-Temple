using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float velocidad = 5f;
    public float gravedad = -9.81f;

    private CharacterController controller;
    private Animator animator;
    private float velocidadVertical;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        Vector2 input = Keyboard.current != null
            ? new Vector2(
                (Keyboard.current.dKey.isPressed ? 1 : 0) -
                (Keyboard.current.aKey.isPressed ? 1 : 0),

                (Keyboard.current.wKey.isPressed ? 1 : 0) -
                (Keyboard.current.sKey.isPressed ? 1 : 0)
            )
            : Vector2.zero;

        Vector3 movimiento = new Vector3(input.x, 0f, input.y);

        if (movimiento.magnitude > 1f)
            movimiento.Normalize();

        // Animación
        if (animator != null)
        {
            animator.SetFloat("Speed", movimiento.magnitude);
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
        if (movimiento != Vector3.zero)
        {
            transform.forward = movimiento;
        }
    }
}