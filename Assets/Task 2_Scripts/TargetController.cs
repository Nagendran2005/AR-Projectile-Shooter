using UnityEngine;

public class TargetController : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Highlight")]
    [SerializeField] private Material highlightMaterial;

    [Header("Destroy Effect")]
    [SerializeField] private ParticleSystem destroyParticle;

    [Header("Destroy Sound")]
    [SerializeField] private AudioClip destroySFX;

    private Material[] originalMaterials;
    private bool isHighlighted = false;
    private bool isDestroyed = false;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        // Store original materials
        originalMaterials = targetRenderer.materials;
    }

    /// <summary>
    /// Highlight ON / OFF
    /// </summary>
    public void SetHighlight(bool value)
    {
        if (isDestroyed)
            return;

        if (isHighlighted == value)
            return;

        isHighlighted = value;

        if (value)
        {
            Material[] mats = new Material[originalMaterials.Length + 1];

            for (int i = 0; i < originalMaterials.Length; i++)
                mats[i] = originalMaterials[i];

            mats[mats.Length - 1] = highlightMaterial;

            targetRenderer.materials = mats;
        }
        else
        {
            targetRenderer.materials = originalMaterials;
        }
    }

    /// <summary>
    /// Called by Projectile when hit.
    /// </summary>
    public void DestroyTarget()
    {
        if (isDestroyed)
            return;

        isDestroyed = true;

        // Remove highlight
        SetHighlight(false);

        // Spawn Particle
        if (destroyParticle != null)
        {
            ParticleSystem effect = Instantiate(
                destroyParticle,
                transform.position,
                Quaternion.identity);

            Destroy(effect.gameObject, 1f);
        }

        // Play SFX
        if (destroySFX != null)
        {
            AudioSource.PlayClipAtPoint(
                destroySFX,
                transform.position);
        }

        // Increase Score
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(10);
        }

        // Notify detector so it doesn't keep a reference
        if (CameraTargetDetector.Instance != null &&
            CameraTargetDetector.Instance.CurrentTarget == this)
        {
            CameraTargetDetector.Instance.ClearCurrentTarget();
        }

        Destroy(gameObject);
    }
}