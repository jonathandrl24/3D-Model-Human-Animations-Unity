using UnityEngine;

/// <summary>
/// Cámara estilo GTA: orbita alrededor del personaje con el mouse,
/// hace zoom con la rueda, y tiene colisión con el entorno.
///
/// SETUP:
///  1. Adjunta este script a la Main Camera.
///  2. Asigna el Transform del personaje en "target".
///  3. (Opcional) Crea un Empty GameObject a la altura del pecho/cabeza
///     del personaje y asígnalo en "pivote" para un punto de mira más natural.
/// </summary>
public class CameraControllerGTA : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════

    [Header("Objetivo")]
    [Tooltip("Transform del personaje")]
    public Transform target;

    [Tooltip("Punto alrededor del cual orbita la cámara (ej: pecho/cabeza). " +
             "Si se deja vacío, se usa 'target' + pivoteOffset.")]
    public Transform pivote;

    [Tooltip("Altura del punto de órbita cuando no hay pivote asignado")]
    public Vector3 pivoteOffset = new Vector3(0f, 1.6f, 0f);

    // ── Ratón ───────────────────────────────────────────────────
    [Header("Control del Mouse")]
    [Tooltip("Sensibilidad horizontal del mouse")]
    [Range(1f, 20f)] public float sensibilidadX = 6f;

    [Tooltip("Sensibilidad vertical del mouse")]
    [Range(1f, 20f)] public float sensibilidadY = 4f;

    [Tooltip("Invertir eje Y")]
    public bool invertirY = false;

    [Tooltip("Ángulo vertical mínimo (mirar hacia abajo)")]
    [Range(-80f, 0f)] public float anguloVerticalMin = -35f;

    [Tooltip("Ángulo vertical máximo (mirar hacia arriba)")]
    [Range(0f, 80f)] public float anguloVerticalMax = 60f;

    // ── Distancia / Zoom ────────────────────────────────────────
    [Header("Distancia y Zoom")]
    [Tooltip("Distancia inicial de la cámara al personaje")]
    [Range(1f, 20f)] public float distanciaInicial = 5f;

    [Tooltip("Distancia mínima (zoom máximo)")]
    [Range(0.5f, 5f)] public float distanciaMin = 1.5f;

    [Tooltip("Distancia máxima (zoom mínimo)")]
    [Range(3f, 30f)] public float distanciaMax = 12f;

    [Tooltip("Velocidad del zoom con la rueda del mouse")]
    [Range(1f, 20f)] public float velocidadZoom = 6f;

    [Tooltip("Suavizado del zoom")]
    [Range(1f, 20f)] public float suavizadoZoom = 8f;

    // ── Suavizado ───────────────────────────────────────────────
    [Header("Suavizado de Movimiento")]
    [Tooltip("Suavizado de rotación horizontal/vertical")]
    [Range(1f, 30f)] public float suavizadoRotacion = 12f;

    [Tooltip("Suavizado de posición al seguir al personaje")]
    [Range(1f, 20f)] public float suavizadoPosicion = 10f;

    // ── Colisión ────────────────────────────────────────────────
    [Header("Colisión con el Entorno")]
    [Tooltip("Activa la detección de obstáculos entre la cámara y el personaje")]
    public bool colisionActivada = true;

    [Tooltip("Capas con las que colisiona la cámara (excluye al personaje)")]
    public LayerMask capasColision = ~0;        // todas por defecto

    [Tooltip("Radio de la esfera del raycast de colisión")]
    [Range(0.05f, 0.5f)] public float radioColision = 0.15f;

    [Tooltip("Distancia mínima a la que la cámara se acerca a una pared")]
    [Range(0.1f, 1f)] public float margenPared = 0.2f;

    [Tooltip("Velocidad con la que la cámara se acerca al muro")]
    [Range(5f, 30f)] public float velocidadAcercamiento = 20f;

    [Tooltip("Velocidad con la que la cámara vuelve a su distancia normal")]
    [Range(1f, 10f)] public float velocidadAlejamiento = 4f;

    // ── Cursor ──────────────────────────────────────────────────
    [Header("Cursor")]
    [Tooltip("Bloquear el cursor al iniciar el juego")]
    public bool bloquearCursorAlInicio = true;

    [Tooltip("Tecla para alternar el bloqueo del cursor")]
    public KeyCode teclaToggleCursor = KeyCode.Escape;

    // ═══════════════════════════════════════════════════════════
    //  PRIVADAS
    // ═══════════════════════════════════════════════════════════

    private float _anguloH;         // rotación horizontal actual
    private float _anguloV;         // rotación vertical actual
    private float _anguloHObj;      // objetivo horizontal (suavizado)
    private float _anguloVObj;      // objetivo vertical  (suavizado)

    private float _distanciaActual;
    private float _distanciaObjetivo;

    private Vector3 _velPosicion = Vector3.zero;
    private bool _cursorBloqueado;

    // ═══════════════════════════════════════════════════════════
    //  UNITY
    // ═══════════════════════════════════════════════════════════

    void Start()
    {
        // Inicializar ángulos con la rotación actual de la cámara
        _anguloH    = transform.eulerAngles.y;
        _anguloV    = transform.eulerAngles.x;
        _anguloHObj = _anguloH;
        _anguloVObj = _anguloV;

        _distanciaActual  = distanciaInicial;
        _distanciaObjetivo = distanciaInicial;

        if (target == null)
            Debug.LogWarning("[CameraControllerGTA] Asigna un Target en el Inspector.");

        SetCursorBloqueado(bloquearCursorAlInicio);
    }

    void Update()
    {
        ManejarCursor();
        LeerInput();
    }

    void LateUpdate()
    {
        if (target == null) return;

        AplicarRotacion();
        AplicarZoom();
        AplicarPosicion();
    }

    // ═══════════════════════════════════════════════════════════
    //  MÉTODOS PRIVADOS
    // ═══════════════════════════════════════════════════════════

    void ManejarCursor()
    {
        if (Input.GetKeyDown(teclaToggleCursor))
            SetCursorBloqueado(!_cursorBloqueado);

        // También bloquear al hacer click izquierdo dentro del juego
        if (!_cursorBloqueado && Input.GetMouseButtonDown(0))
            SetCursorBloqueado(true);
    }

    void LeerInput()
    {
        if (!_cursorBloqueado) return;

        // ── Rotación ──────────────────────────────────────────
        float mouseX = Input.GetAxis("Mouse X") * sensibilidadX;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidadY * (invertirY ? 1f : -1f);

        _anguloHObj += mouseX;
        _anguloVObj  = Mathf.Clamp(_anguloVObj + mouseY, anguloVerticalMin, anguloVerticalMax);

        // ── Zoom ──────────────────────────────────────────────
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        _distanciaObjetivo -= scroll * velocidadZoom;
        _distanciaObjetivo  = Mathf.Clamp(_distanciaObjetivo, distanciaMin, distanciaMax);
    }

    void AplicarRotacion()
    {
        // Suavizado de ángulos
        _anguloH = Mathf.LerpAngle(_anguloH, _anguloHObj, suavizadoRotacion * Time.deltaTime);
        _anguloV = Mathf.LerpAngle(_anguloV, _anguloVObj, suavizadoRotacion * Time.deltaTime);
    }

    void AplicarZoom()
    {
        // Suavizado de distancia (se corregirá si hay colisión)
        _distanciaActual = Mathf.Lerp(_distanciaActual, _distanciaObjetivo, suavizadoZoom * Time.deltaTime);
    }

    void AplicarPosicion()
    {
        // ── Punto de pivote ───────────────────────────────────
        Vector3 puntoPivote = pivote != null
            ? pivote.position
            : target.position + pivoteOffset;

        // ── Dirección de la cámara a partir de los ángulos ───
        Quaternion rotacion = Quaternion.Euler(_anguloV, _anguloH, 0f);
        Vector3 direccion   = rotacion * Vector3.back;   // "atrás" del personaje

        // ── Colisión ──────────────────────────────────────────
        float distanciaFinal = _distanciaActual;

        if (colisionActivada)
        {
            // SphereCast desde el pivote hacia la posición deseada de la cámara
            RaycastHit hit;
            if (Physics.SphereCast(
                    puntoPivote,
                    radioColision,
                    direccion,
                    out hit,
                    _distanciaActual,
                    capasColision))
            {
                // Acortar distancia para que la cámara no atraviese la pared
                float distanciaSegura = Mathf.Max(hit.distance - margenPared, distanciaMin);
                distanciaFinal = Mathf.MoveTowards(distanciaFinal, distanciaSegura,
                                                    velocidadAcercamiento * Time.deltaTime);
            }
            else
            {
                // Sin obstáculo: volver a la distancia objetivo lentamente
                distanciaFinal = Mathf.MoveTowards(distanciaFinal, _distanciaObjetivo,
                                                    velocidadAlejamiento * Time.deltaTime);
            }

            _distanciaActual = distanciaFinal;
        }

        // ── Posición y rotación finales ───────────────────────
        Vector3 posicionDeseada = puntoPivote + direccion * distanciaFinal;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            posicionDeseada,
            ref _velPosicion,
            1f / suavizadoPosicion);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rotacion,
            suavizadoRotacion * Time.deltaTime);
    }

    // ── Utilidad ──────────────────────────────────────────────
    void SetCursorBloqueado(bool bloqueado)
    {
        _cursorBloqueado = bloqueado;
        Cursor.lockState = bloqueado ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !bloqueado;
    }

    /// <summary>
    /// Devuelve la dirección horizontal hacia la que apunta la cámara.
    /// Útil para orientar el movimiento del personaje según la cámara.
    /// </summary>
    public Vector3 DireccionFrente()
    {
        Vector3 frente = Quaternion.Euler(0f, _anguloH, 0f) * Vector3.forward;
        return frente.normalized;
    }

    /// <summary>
    /// Devuelve la dirección lateral de la cámara (para strafe).
    /// </summary>
    public Vector3 DireccionDerecha()
    {
        return Quaternion.Euler(0f, _anguloH, 0f) * Vector3.right;
    }

    // ── Gizmos ────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Vector3 pivot = pivote != null ? pivote.position : target.position + pivoteOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pivot, 0.15f);
        Gizmos.DrawLine(pivot, transform.position);
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, radioColision);
    }
}
