using System.Collections;
using UnityEngine;
using TMPro;

public class ARLiquidCapsule : MonoBehaviour
{
    [Header("Target Health Constraints")]
    [SerializeField] private int maxStrikes = 3;

    [Header("Liquid Core Shader Configuration")]
    [Tooltip("CRUCIAL: Drag the inner capsule child object that has your 'liqudeshader' material directly into this slot!")]
    [SerializeField] private Renderer liquidMeshRenderer;

    [Tooltip("This matches the internal Reference string from your graph properties blackboard.")]
    [SerializeField] private string fillReferenceName = "_fill";
    [SerializeField] private float defaultFillAmount = 1f;

    [Header("Dynamic Overhead Text")]
    [SerializeField] private TextMeshProUGUI contentRatioLabel;

    [Header("Structural Deflection (Hit Shake)")]
    [SerializeField] private Transform animatedModelNode;
    [SerializeField] private float shakeTimeSpan = 0.15f;
    [SerializeField] private float shakeIntensity = 0.12f;

    [Header("Feedback Triggers")]
    [SerializeField] private AudioClip impactAudio;
    [SerializeField] private ParticleSystem ruptureVFX;
    [SerializeField] private AudioClip ruptureSFX;

    [HideInInspector] public TextMeshProUGUI hitCountText;

    private Material dynamicMaterial;
    private int accruedStrikes = 0;
    private bool internalDestroyActive = false;
    private Vector3 baselineLocalPos;
    private Coroutine activeWobbleRoutine;

    private void Awake()
    {
        // Fallback detection logic if the inspector slot was left empty
        if (liquidMeshRenderer == null)
            liquidMeshRenderer = GetComponentInChildren<Renderer>();

        if (liquidMeshRenderer != null)
        {
            // Instantiates a unique material clone for this specific target instance at runtime
            dynamicMaterial = liquidMeshRenderer.material;
            dynamicMaterial.SetFloat(fillReferenceName, defaultFillAmount);
        }
        else
        {
            Debug.LogError($"[ARLiquidCapsule] Missing mesh renderer on {gameObject.name}! Manually drag the child liquid object into the slot.", this);
        }

        if (animatedModelNode == null)
            animatedModelNode = transform;

        baselineLocalPos = animatedModelNode.localPosition;
    }

    private void Start()
    {
        RefreshCapsuleState();
    }

    public bool RegisterImpact()
    {
        if (internalDestroyActive) return false;

        accruedStrikes++;
        RefreshCapsuleState();

        if (activeWobbleRoutine != null) StopCoroutine(activeWobbleRoutine);
        activeWobbleRoutine = StartCoroutine(ExecuteStructuralWobble());

        if (impactAudio != null && accruedStrikes < maxStrikes)
        {
            AudioSource.PlayClipAtPoint(impactAudio, transform.position);
        }

        if (accruedStrikes >= maxStrikes)
        {
            TriggerRuptureSequence();
            return true;
        }

        return false;
    }

    private void RefreshCapsuleState()
    {
        // Calculates smooth descending steps: 1.0 (0 hits) -> 0.66 (1 hit) -> 0.33 (2 hits) -> 0.0 (3 hits)
        float ratioRemaining = Mathf.Clamp01(1f - ((float)accruedStrikes / maxStrikes));

        if (dynamicMaterial != null)
        {
            float updatedFill = ratioRemaining * defaultFillAmount;
            dynamicMaterial.SetFloat(fillReferenceName, updatedFill);
        }

        if (contentRatioLabel != null)
        {
            int displayPercentage = Mathf.RoundToInt(ratioRemaining * 100f);
            contentRatioLabel.text = $"{displayPercentage}%";
        }
    }

    private IEnumerator ExecuteStructuralWobble()
    {
        float elapsed = 0f;

        while (elapsed < shakeTimeSpan)
        {
            elapsed += Time.deltaTime;

            float randomX = Random.Range(-1f, 1f) * shakeIntensity;
            float randomY = Random.Range(-1f, 1f) * shakeIntensity;

            animatedModelNode.localPosition = baselineLocalPos + new Vector3(randomX, randomY, 0f);
            yield return null;
        }

        animatedModelNode.localPosition = baselineLocalPos;
    }

    private void TriggerRuptureSequence()
    {
        internalDestroyActive = true;

        if (ruptureVFX != null)
        {
            ParticleSystem fxInstance = Instantiate(ruptureVFX, transform.position, Quaternion.identity);
            fxInstance.Play();
            Destroy(fxInstance.gameObject, 1.5f);
        }

        if (ruptureSFX != null)
        {
            AudioSource.PlayClipAtPoint(ruptureSFX, transform.position);
        }

        Destroy(gameObject);
    }
}