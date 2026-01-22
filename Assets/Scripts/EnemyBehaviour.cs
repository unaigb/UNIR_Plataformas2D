using UnityEngine;
using System.Collections;

public class EnemyBehaviour : MonoBehaviour
{
    [Header("Configuración Tipo de Enemigo")]
    public bool esMiedoso = false;
    public bool esAgresivo = true;
    public bool puedeSaltar = true;

    [Header("Configuración Visual")]
    public bool iniciarMirandoIzquierda = false;

    [Header("Loot / Recompensas")]
    public bool tieneLaLlave = false;
    public GameObject prefabLlave;

    [Header("Estadísticas")]
    public float velocidad = 3f;
    public float velocidadHuir = 4.5f;
    public int vidas = 2;
    public float radioDeteccion = 6f;
    public float radioAtaque = 1.2f;
    public float radioMiedoBomba = 4f;
    public float cooldownAtaque = 1.5f;
    public int puntos = 100;

    [Header("IA de Salto")]
    public float fuerzaSalto = 6f;
    public float distanciaDetector = 1f;
    public float alturaDetector = 0.5f;
    public float cooldownSalto = 1.0f;
    private float tiempoUltimoSalto;

    [Header("Referencias Emotes (Hijos)")]
    public Animator animAlerta;
    public Animator animMiedo;

    [Header("Físicas y Suelo")]
    public Transform groundCheck;
    public float radioSuelo = 0.2f;
    public LayerMask capaSuelo;
    public LayerMask capaBomba;

    // Referencias internas
    private Rigidbody2D rb;
    private Animator anim;
    private Transform playerTransform;
    private bool tocandoSuelo;
    private bool estaMuerto = false;
    private bool recibiendoDano = false;
    private bool huyendo = false;
    private float tiempoUltimoAtaque;
    private Vector3 escalaOriginal;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        escalaOriginal = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

        if (iniciarMirandoIzquierda)
        {
            transform.localScale = new Vector3(-escalaOriginal.x, escalaOriginal.y, escalaOriginal.z);
        }
        else
        {
            transform.localScale = escalaOriginal;
        }

        BuscarJugador();

        if (animAlerta) animAlerta.gameObject.SetActive(false);
        if (animMiedo) animMiedo.gameObject.SetActive(false);
    }

    void Update()
    {
        if (estaMuerto) return;

        anim.SetFloat("YSpeed", rb.linearVelocity.y);

        if (playerTransform == null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            anim.SetBool("Run", false);

            BuscarJugador();

            if (playerTransform == null) return;
        }

        tocandoSuelo = Physics2D.OverlapCircle(groundCheck.position, 0.2f, capaSuelo);
        ActualizarAnimacionesFisicas();

        if (recibiendoDano)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        TomarDecisionesIA();
    }

    void BuscarJugador()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) playerTransform = p.transform;
    }

    void FixedUpdate()
    {
        tocandoSuelo = Physics2D.OverlapCircle(groundCheck.position, radioSuelo, capaSuelo);
        ActualizarAnimacionesFisicas();
    }

    void ActualizarAnimacionesFisicas()
    {
        if (puedeSaltar)
        {
            anim.SetBool("IsGrounded", tocandoSuelo);
            anim.SetFloat("YSpeed", rb.linearVelocity.y);
            anim.SetBool("Scared", huyendo);
        }

        anim.SetBool("Run", Mathf.Abs(rb.linearVelocity.x) > 0.1f); 
    }

    void TomarDecisionesIA()
    {
        // 1. PRIORIDAD: ¿Bomba cerca?
        Transform bombaCercana = DetectarBomba();
        if (esMiedoso && bombaCercana != null)
        {
            huyendo = true;
            Huir(bombaCercana);
            GestionarEmotes(animMiedo);
            return;
        } else
        {
            huyendo = false;
        }

        // 2. DETECCIÓN JUGADOR
        if (playerTransform == null) return;
        float distanciaJugador = Vector2.Distance(transform.position, playerTransform.position);

        if (distanciaJugador < radioDeteccion && !huyendo)
        {
            GestionarEmotes(animAlerta);

            if (esAgresivo && distanciaJugador <= radioAtaque)
            {
                Atacar();
            }
            else
            {
                Perseguir();
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            GestionarEmotes(null);
        }
    }

    // --- COMPORTAMIENTOS ---

    void Perseguir()
    {
        float dirX = (playerTransform.position.x > transform.position.x) ? 1 : -1;
        Moverse(dirX, velocidad);

        if (puedeSaltar)
        {
            IntentarSaltarObstaculos();
        }
    }

    void Huir(Transform peligro)
    {
        float dirX = (peligro.position.x > transform.position.x) ? -1 : 1;
        Moverse(dirX, velocidadHuir);
    }

    void Atacar()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        if (Time.time > tiempoUltimoAtaque + cooldownAtaque)
        {
            anim.SetTrigger("Attack");
            tiempoUltimoAtaque = Time.time;
            StartCoroutine(HacerDanoDelay());
        }
    }

    IEnumerator HacerDanoDelay()
    {
        yield return new WaitForSeconds(0.4f);
        if (!estaMuerto && playerTransform != null &&
            Vector2.Distance(transform.position, playerTransform.position) <= radioAtaque)
        {
            var playerScript = playerTransform.GetComponent<PlayerController>();
            if (playerScript != null) playerScript.RecibirDano(1);
        }
    }

    void Moverse(float dir, float vel)
    {
        rb.linearVelocity = new Vector2(dir * vel, rb.linearVelocity.y);
        float xSize = Mathf.Abs(escalaOriginal.x);
        if (dir > 0)
        {
            transform.localScale = new Vector3(xSize, escalaOriginal.y, escalaOriginal.z);
            CorregirOrientacionHijos(1);
        }
        else
        {
            transform.localScale = new Vector3(-xSize, escalaOriginal.y, escalaOriginal.z);
            CorregirOrientacionHijos(-1);
        }
    }

    void CorregirOrientacionHijos(float dirPadre)
    {
        if (animAlerta)
        {
            Vector3 s = animAlerta.transform.localScale;
            animAlerta.transform.localScale = new Vector3(Mathf.Abs(s.x) * dirPadre, s.y, s.z);
        }

        if (animMiedo)
        {
            Vector3 s = animMiedo.transform.localScale;
            animMiedo.transform.localScale = new Vector3(Mathf.Abs(s.x) * dirPadre, s.y, s.z);
        }
    }

    // --- SISTEMA DE DAÑO Y MUERTE ---

    public void RecibirDano(int cantidad)
    {
        huyendo = false;
        if (estaMuerto) return;
        vidas -= cantidad;
        recibiendoDano = true;

        if (vidas <= 0) Morir();
        else
        {
            anim.SetTrigger("Hit");
            StartCoroutine(RecuperarHit());
        }
    }

    IEnumerator RecuperarHit()
    {
        yield return new WaitForSeconds(0.4f);
        recibiendoDano = false;
    }

    void Morir()
    {
        huyendo = false;
        estaMuerto = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        GetComponent<Collider2D>().enabled = false;
        GestionarEmotes(null);
        
        anim.SetTrigger("DeadHit");

        if (GameManager.Instance != null) GameManager.Instance.GanarPuntos(puntos);

        if (tieneLaLlave && GameManager.Instance != null && !GameManager.Instance.tieneLlave)
        {
            if (prefabLlave != null)
            {
                Instantiate(prefabLlave, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            }
        }

        Destroy(gameObject, 2f);
    }

    void IntentarSaltarObstaculos()
    {
        if (!tocandoSuelo || Time.time < tiempoUltimoSalto + cooldownSalto) return;

        float direccion = Mathf.Sign(rb.linearVelocity.x);

        if (Mathf.Abs(rb.linearVelocity.x) < 0.1f)
        {
            direccion = Mathf.Sign(transform.localScale.x);
        }

        Vector2 origen = new Vector2(transform.position.x, transform.position.y + alturaDetector);

        RaycastHit2D obstaculo = Physics2D.Raycast(origen, Vector2.right * direccion, distanciaDetector, capaSuelo);

        if (obstaculo.collider != null)
        {
            // if (playerTransform.position.y > transform.position.y) 

            Saltar();
        }
    }

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
        tiempoUltimoSalto = Time.time;
    }

    // --- UTILIDADES ---

    Transform DetectarBomba()
    {
        Collider2D col = Physics2D.OverlapCircle(transform.position, radioMiedoBomba, capaBomba);
        return col ? col.transform : null;
    }

    void GestionarEmotes(Animator emoteAActivar)
    {
        // Si queremos activar Alerta
        if (emoteAActivar == animAlerta)
        {
            ActivarEmote(animAlerta);
            DesactivarEmote(animMiedo);
        }
        // Si queremos activar Miedo
        else if (emoteAActivar == animMiedo)
        {
            ActivarEmote(animMiedo);
            DesactivarEmote(animAlerta);
        }
        // Si queremos apagar todo
        else
        {
            DesactivarEmote(animAlerta);
            DesactivarEmote(animMiedo);
        }
    }

    void ActivarEmote(Animator emote)
    {
        if (emote == null) return;
        if (!emote.gameObject.activeSelf)
        {
            emote.gameObject.SetActive(true);
        }
    }

    void DesactivarEmote(Animator emote)
    {
        if (emote == null) return;
        if (emote.gameObject.activeSelf)
        {
            StartCoroutine(ProcesoCerrarEmote(emote));
        }
    }

    IEnumerator ProcesoCerrarEmote(Animator emote)
    {
        if (emote.gameObject.activeSelf)
        {
            emote.SetTrigger("Close");

            yield return new WaitForSeconds(0.3f);

            emote.gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioDeteccion);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioAtaque);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, radioMiedoBomba);
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, radioSuelo);
        }

        Gizmos.color = Color.magenta;
        Vector3 origen = new Vector3(transform.position.x, transform.position.y + alturaDetector, 0);
        Gizmos.DrawLine(origen, origen + Vector3.right * distanciaDetector);
        Gizmos.DrawLine(origen, origen + Vector3.left * distanciaDetector);
    }
}