using UnityEngine;

/// <summary>
/// Script de seguimiento de cámara para Unity.
/// Adjunta este script a la cámara principal (Main Camera).
/// Asigna el Transform del personaje en el Inspector.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("Transform del personaje a seguir")]
    public Transform target;

    [Header("Offset y posición")]
    [Tooltip("Distancia y ángulo de la cámara respecto al personaje")]
    public Vector3 offset = new Vector3(0f, 5f, -8f);

    [Tooltip("Rotar el offset junto con el personaje (útil para juegos 3D con rotación de personaje)")]
    public bool offsetRelativoAlPersonaje = false;

    [Header("Suavizado")]
    [Tooltip("Qué tan suave sigue la cámara (menor = más suave, mayor = más rígido)")]
    [Range(1f, 20f)]
    public float velocidadSeguimiento = 5f;

    [Tooltip("Suavizado de rotación (si se usa lookAt)")]
    [Range(1f, 20f)]
    public float velocidadRotacion = 5f;

    [Header("Mirar al personaje")]
    [Tooltip("La cámara siempre mirará hacia el personaje")]
    public bool mirarAlPersonaje = true;

    [Tooltip("Punto de mira del personaje (ej: cabeza). Si está vacío, usa el Transform del target)")]
    public Transform puntoMira;

    [Header("Límites opcionales")]
    [Tooltip("Limitar movimiento de la cámara en el eje Y (útil para mapas 2D o plataformeros)")]
    public bool limitarEjeY = false;
    public float yMinimo = -10f;
    public float yMaximo = 50f;

    // ─────────────────────────────────────────────
    private Vector3 _velocidad = Vector3.zero;

    void Start()
    {
        if (target == null)
            Debug.LogWarning("[CameraFollow] No hay ningún target asignado. Asigna el Transform del personaje en el Inspector.");

        // Si no se definió punto de mira, usar el propio target
        if (puntoMira == null && target != null)
            puntoMira = target;
    }

    // LateUpdate garantiza que el personaje ya se movió en este frame
    void LateUpdate()
    {
        if (target == null) return;

        // ── 1. Calcular posición deseada ──────────────────────────────
        Vector3 posicionDeseada;

        if (offsetRelativoAlPersonaje)
            posicionDeseada = target.position + target.TransformDirection(offset);
        else
            posicionDeseada = target.position + offset;

        // Aplicar límite en Y si está activado
        if (limitarEjeY)
            posicionDeseada.y = Mathf.Clamp(posicionDeseada.y, yMinimo, yMaximo);

        // ── 2. Mover cámara suavemente ────────────────────────────────
        transform.position = Vector3.SmoothDamp(
            transform.position,
            posicionDeseada,
            ref _velocidad,
            1f / velocidadSeguimiento   // tiempo de suavizado
        );

        // ── 3. Rotar hacia el personaje (opcional) ────────────────────
        if (mirarAlPersonaje && puntoMira != null)
        {
            Quaternion rotacionDeseada = Quaternion.LookRotation(puntoMira.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, velocidadRotacion * Time.deltaTime);
        }
    }

    // ── Gizmo para ver el offset en el editor ─────────────────────────
    void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Gizmos.color = Color.cyan;
        Vector3 posGizmo = offsetRelativoAlPersonaje
            ? target.position + target.TransformDirection(offset)
            : target.position + offset;

        Gizmos.DrawWireSphere(posGizmo, 0.3f);
        Gizmos.DrawLine(target.position, posGizmo);
    }
}
