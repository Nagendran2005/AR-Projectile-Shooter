using UnityEngine;
using TMPro;

public class TargetController : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHits = 3;

    [Header("Renderer")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Highlight")]
    [SerializeField] private Material highlightMaterial;

    [Header("Damage & Destroy Effects")]
    [SerializeField] private AudioClip hitSFX;
    [SerializeField] private ParticleSystem destroyParticle;
    [SerializeField] private AudioClip destroySFX;

    // REMOVED [SerializeField] - This is now assigned automatically dynamically!
    [HideInInspector] public TextMeshProUGUI hitCountText;

    private Material[] originalMaterials;
    private bool isHighlighted = false;
    private bool isDestroyed = false;
    private int currentHits = 0;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        originalMaterials = targetRenderer.materials;
    }

    private void Start()
    {
        UpdateHitCountUI();
    }

    public void SetHighlight(bool value)
    {
        if (isDestroyed) return;
        if (isHighlighted == value) return;

        isHighlighted = value;

        if (value)
        {
            Material[] mats = new Material[originalMaterials.Length + 1];
            for (int i = 0; i < originalMaterials.Length; i++)
                mats[i] = originalMaterials[i];

            mats[mats.Length - 1] = highlightMaterial;
            targetRenderer.materials = mats;

            UpdateHitCountUI();
        }
        else
        {
            targetRenderer.materials = originalMaterials;
        }
    }

    public bool TakeHit()
    {
        if (isDestroyed) return false;

        currentHits++;
        UpdateHitCountUI();

        if (hitSFX != null && currentHits < maxHits)
        {
            AudioSource.PlayClipAtPoint(hitSFX, transform.position);
        }

        if (currentHits >= maxHits)
        {
            ExecuteDestroy();
            return true;
        }

        return false;
    }

    private void UpdateHitCountUI()
    {
        if (hitCountText != null)
        {
            hitCountText.text = $"Hits: {currentHits}/{maxHits}";
        }
    }

    private void ExecuteDestroy()
    {
        isDestroyed = true;

        if (hitCountText != null)
        {
            hitCountText.text = "Hits: 0/3";
        }

        SetHighlight(false);

        if (destroyParticle != null)
        {
            ParticleSystem effect = Instantiate(destroyParticle, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 1f);
        }

        if (destroySFX != null)
        {
            AudioSource.PlayClipAtPoint(destroySFX, transform.position);
        }

        if (CameraTargetDetector.Instance != null && CameraTargetDetector.Instance.CurrentTarget == this)
        {
            CameraTargetDetector.Instance.ClearCurrentTarget();
        }

        Destroy(gameObject);
    }
}