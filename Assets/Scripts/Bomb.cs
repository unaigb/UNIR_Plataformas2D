using UnityEngine;
using System.Collections;

public class Bomba : MonoBehaviour
{
    [Header("Configuración")]
    public float tiempoParaExplotar = 3f;
    public float duracionFrameDaño = 0.1f;
    public float tiempoDesaparecer = 0.6f;

    [Header("Daño y Radios")]
    public float radioCritico = 1.0f; // Distancia para hacer 2 de daño
    public int danoBase = 1;
    public int danoCritico = 2;

    [Header("Detección de Suelo")]
    public Transform groundCheck;
    public float radioSuelo = 0.2f;
    public LayerMask capaSuelo;

    [Header("Combate")]
    public LayerMask capaEnemigos;

    // Referencias internas
    private Animator anim;
    private Rigidbody2D rb;
    private CircleCollider2D colliderSolido;
    private CircleCollider2D colliderTrigger;

    // Banderas
    private bool cuentaAtrasIniciada = false;
    private bool tocandoSuelo = false;
    private bool haExplotado = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        CircleCollider2D[] colliders = GetComponents<CircleCollider2D>();
        foreach (CircleCollider2D c in colliders)
        {
            if (c.isTrigger) colliderTrigger = c;
            else colliderSolido = c;
        }

        if (colliderTrigger != null) colliderTrigger.enabled = false;
    }

    void FixedUpdate()
    {
        if (haExplotado) return;

        tocandoSuelo = Physics2D.OverlapCircle(groundCheck.position, radioSuelo, capaSuelo);
        anim.SetBool("IsGrounded", tocandoSuelo);

        if (tocandoSuelo && !cuentaAtrasIniciada)
        {
            cuentaAtrasIniciada = true;
            StartCoroutine(CuentaAtras());
        }
    }

    IEnumerator CuentaAtras()
    {
        yield return new WaitForSeconds(tiempoParaExplotar);
        if (!haExplotado) StartCoroutine(ProcesoExplosion());
    }

    IEnumerator ProcesoExplosion()
    {
        haExplotado = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        colliderSolido.enabled = false;

        anim.SetTrigger("ExplosionTrigger");

        if (colliderTrigger != null)
        {
            colliderTrigger.enabled = true;
            yield return new WaitForSeconds(duracionFrameDaño);
            colliderTrigger.enabled = false;
        }

        yield return new WaitForSeconds(tiempoDesaparecer);
        Destroy(gameObject);
    }

    // --- NUEVA LÓGICA DE DAÑO ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & capaEnemigos) != 0 || other.CompareTag("Enemy"))
        {
            EnemyBehaviour scriptEnemigo = other.GetComponent<EnemyBehaviour>();

            if (scriptEnemigo != null)
            {
                // 2. CORRECCIÓN CENTRO: Usamos el centro del collider trigger
                Vector2 centroExplosion = transform.position; // Por defecto

                if (colliderTrigger != null)
                {
                    // bounds.center nos da la posición real del círculo verde del collider
                    centroExplosion = colliderTrigger.bounds.center;
                }

                // Calculamos distancia desde ese centro corregido
                float distancia = Vector2.Distance(centroExplosion, other.transform.position);

                int danoFinal = (distancia <= radioCritico) ? danoCritico : danoBase;

                scriptEnemigo.RecibirDano(danoFinal);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, radioSuelo);
        }

        // Dibujamos el radio crítico usando el centro corregido si es posible
        Gizmos.color = Color.red;

        // Truco para dibujarlo bien en el editor antes de darle al Play
        // Buscamos el collider trigger manualmente si no estamos en Play Mode
        if (!Application.isPlaying)
        {
            CircleCollider2D[] colls = GetComponents<CircleCollider2D>();
            foreach (var c in colls) if (c.isTrigger)
                {
                    // Dibujamos donde estaría el centro según el offset
                    Vector3 centroEstimado = transform.TransformPoint(c.offset);
                    Gizmos.DrawWireSphere(centroEstimado, radioCritico);
                    return;
                }
        }
        else if (colliderTrigger != null)
        {
            // En Play Mode usamos el bounds.center real
            Gizmos.DrawWireSphere(colliderTrigger.bounds.center, radioCritico);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, radioCritico);
        }
    }
}