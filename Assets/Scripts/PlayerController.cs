using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.Cinemachine;

public class PlayerController : MonoBehaviour
{
    [Header("Configuración")]
    public float velocidad = 5f;
    public float fuerzaSalto = 6.5f;

    [Header("Detección de Suelo")]
    public Transform groundCheck;
    public float radioSuelo = 0.2f;
    public LayerMask capaSuelo;

    [Header("Salud del Jugador")]
    public int vidas = 3;
    public float tiempoInvencible = 1.5f;
    private bool esInvencible = false;
    private bool estaMuerto = false;

    // Referencias internas
    private Rigidbody2D rb;
    private Controles inputs;
    private Animator anim;

    private Vector2 movimientoInput;
    private bool tocandoSuelo;
    private bool mirandoDerecha = true;

    [Header("Combate - Bombas")]
    public GameObject bombaPrefab;
    public Transform puntoLanzamiento;

    [Header("Configuración Disparo")]
    public float fuerzaMinima = 5f;
    public float fuerzaMaxima = 15f;
    public float tiempoCargaMaximo = 1.5f;
    public float cooldownDisparo = 0.5f;

    [Header("Feedback Visual")]
    public GameObject barraFuerzaObj;
    public Animator barraAnimator;
    public float duracionAnimCierre = 0.2f;

    // Variables internas para la lógica
    private bool cargandoDisparo = false;
    private float tiempoInicioCarga;
    private float tiempoUltimoDisparo = -999f;
    private bool inputsActivos = true;

    private Door puertaActual;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        inputs = new Controles();

        // --- CONFIGURACIÓN DE INPUTS ---

        inputs.Player.Jump.performed += Contexto => Saltar();

        inputs.Player.Attack.started += AlDisparar;
        inputs.Player.Attack.canceled += AlDisparar;

        inputs.Player.Interact.performed += _ => IntentarInteractuar();

        if (GameManager.Instance != null)
        {
            vidas = GameManager.Instance.GetVidasActuales();
        }
    }

    void Start()
    {
        CinemachineCamera camara = Object.FindAnyObjectByType<CinemachineCamera>();

        if (camara != null)
        {
            camara.Follow = this.transform;
        }
        else
        {
            Debug.LogError("¡No encuentro ninguna CinemachineCamera en la escena!");
        }
        if (barraFuerzaObj != null) barraFuerzaObj.SetActive(false);
    }

    private void OnEnable() => inputs.Enable();
    private void OnDisable() 
    {
        GetComponent<SpriteRenderer>().enabled = true;
        esInvencible = false;
        inputs.Disable();
    }

    void Update()
    {
        if (!inputsActivos || estaMuerto)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        movimientoInput = inputs.Player.Move.ReadValue<Vector2>();

        if (movimientoInput.x > 0 && !mirandoDerecha) Voltear();
        else if (movimientoInput.x < 0 && mirandoDerecha) Voltear();

        anim.SetFloat("Speed", Mathf.Abs(movimientoInput.x));
        anim.SetBool("IsGrounded", tocandoSuelo);
        anim.SetFloat("YSpeed", rb.linearVelocity.y);
    }

    void FixedUpdate()
    {
        if (!inputsActivos || estaMuerto)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        tocandoSuelo = Physics2D.OverlapCircle(groundCheck.position, radioSuelo, capaSuelo);
        rb.linearVelocity = new Vector2(movimientoInput.x * velocidad, rb.linearVelocity.y);
    }

    // --- LÓGICA DE SALTO Y MOVIMIENTO ---

    void Saltar()
    {
        if (tocandoSuelo)
        {
            StartCoroutine(ProcesoSalto());
        }
    }

    IEnumerator ProcesoSalto()
    {
        anim.SetTrigger("JumpTrigger");
        yield return new WaitForSeconds(0.1f);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
    }

    void Voltear()
    {
        mirandoDerecha = !mirandoDerecha;
        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;
    }

    // --- LÓGICA DE COMBATE ---

    public void AlDisparar(InputAction.CallbackContext context)
    {
        if (!inputsActivos || estaMuerto) return;

        // 1. AL PULSAR EL BOTÓN (INICIAR CARGA)
        if (context.started)
        {
            if (Time.time < tiempoUltimoDisparo + cooldownDisparo) return;

            cargandoDisparo = true;
            tiempoInicioCarga = Time.time;

            if (barraFuerzaObj != null)
            {
                barraFuerzaObj.SetActive(true);
                StopAllCoroutines();

                if (barraAnimator != null)
                {
                    barraAnimator.SetBool("Loading", true);

                    barraAnimator.speed = 1f / tiempoCargaMaximo;

                    barraAnimator.Play("Charging Bar", 0, 0f);
                }
            }
        }

        // 2. AL SOLTAR EL BOTÓN (DISPARAR)
        if (context.canceled && cargandoDisparo)
        {
            LanzarBomba();

            cargandoDisparo = false;
            if (barraFuerzaObj != null && barraAnimator != null)
            {
                barraAnimator.SetBool("Loading", false);

                barraAnimator.speed = 1f;

                StartCoroutine(OcultarBarraRoutine());
            }
            else if (barraFuerzaObj != null)
            {
                barraFuerzaObj.SetActive(false);
            }
        }
    }

    void LanzarBomba()
    {
        float tiempoPresionado = Time.time - tiempoInicioCarga;
        float porcentajeCarga = Mathf.Clamp01(tiempoPresionado / tiempoCargaMaximo);
        float fuerzaFinal = Mathf.Lerp(fuerzaMinima, fuerzaMaxima, porcentajeCarga);

        GameObject bomba = Instantiate(bombaPrefab, puntoLanzamiento.position, Quaternion.identity);
        Rigidbody2D rbBomba = bomba.GetComponent<Rigidbody2D>();

        float direccionX = transform.localScale.x > 0 ? 1 : -1;
        Vector2 direccionLanzamiento = new Vector2(direccionX, 1).normalized;

        rbBomba.linearVelocity = direccionLanzamiento * fuerzaFinal;

        tiempoUltimoDisparo = Time.time;
    }

    IEnumerator OcultarBarraRoutine()
    {
        yield return new WaitForSeconds(duracionAnimCierre);

        if (barraFuerzaObj != null)
        {
            barraFuerzaObj.SetActive(false);
        }
    }

    public void RecibirDano(int dano)
    {
        UIController uIController = FindAnyObjectByType<UIController>();
        if (estaMuerto || esInvencible) return;

        if (uIController != null && GameManager.Instance != null)
        {
            GameManager.Instance.PerderVida();
            vidas = GameManager.Instance.GetVidasActuales();
            uIController.ActualizarVidas(vidas);

            if (vidas > 0)
            {
                anim.SetTrigger("Hit");
            }
            else
            {
                Morir();
            }
        }
    }

    void Morir()
    {
        if (estaMuerto) return;
        estaMuerto = true;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        Collider2D[] colls = GetComponents<Collider2D>();
        foreach (var c in colls) c.enabled = false;

        anim.SetTrigger("DeadHit");

        StartCoroutine(SecuenciaGameOver());
    }

    IEnumerator SecuenciaGameOver()
    {
        yield return new WaitForSeconds(2f);

        GameManager manager = FindAnyObjectByType<GameManager>();
        if (manager != null)
        {
            manager.GameOver();
        }

        gameObject.SetActive(false);
    }

    void IntentarInteractuar()
    {
        if (estaMuerto) return;

        if (puertaActual != null)
        {
            puertaActual.IntentarAbrir();
        }
    }

    public void SetPuertaCercana(Door puerta)
    {
        puertaActual = puerta;
    }

    public void AnimacionEntrarPuerta()
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        anim.Play("Door-in");
    }

    public void AnimacionSalirPuerta()
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        anim.Play("Door-out");
        gameObject.SetActive(false);
        StartCoroutine(HabilitarControlSalida());
    }

    IEnumerator HabilitarControlSalida()
    {
        yield return new WaitForSeconds(1f);
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    public void DesactivarInput()
    {
        inputsActivos = false;
        rb.linearVelocity = Vector2.zero; // Frenamos al personaje
        anim.SetFloat("Speed", 0); // Opcional: poner animación de Idle
    }

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, radioSuelo);
        }
    }
}