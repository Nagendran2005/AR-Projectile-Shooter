using UnityEngine;
using TMPro;

public class FocusElementController : MonoBehaviour
{
    [Header("Interaction Thresholds")]
    [SerializeField] private int structuralDurability = 3;

    [Header("Visual Components")]
    [SerializeField] private Renderer elementMeshRenderer;
    [SerializeField] private Material selectionGlowMaterial;

    [Header("Floating Information HUD")]
    [Tooltip("Drag your world-space text canvas prefab here.")]
    [SerializeField] private GameObject identityPopupPrefab;
    [Tooltip("The vertical height offset on the Y-axis above the element center pivot.")]
    [SerializeField] private float verticalSpacialOffset = 1.5f;
    [Tooltip("The custom descriptive name string for this specific element instance.")]
    [SerializeField] private string descriptiveIdentifier = "Core Component";

    [Header("Isolated Sound Profiles")]
    [Tooltip("Plays the exact moment the camera focuses on this unique element.")]
    [SerializeField] private AudioClip focusAcquiredAudio;
    [SerializeField] private AudioClip impactAudio;
    [SerializeField] private ParticleSystem terminationVFX;
    [SerializeField] private AudioClip terminationAudio;

    [HideInInspector] public TextMeshProUGUI trackingCounterLabel;

    private Material[] defaultMaterialCache;
    private GameObject runtimePopupInstance;
    private TextMeshProUGUI runtimeNameLabel;

    private bool isCurrentlyFocused = false;
    private bool isSystemTerminated = false;
    private int accumulatedImpacts = 0;

    private void Awake()
    {
        if (elementMeshRenderer == null)
            elementMeshRenderer = GetComponentInChildren<Renderer>();

        // Cache base material configuration textures
        defaultMaterialCache = elementMeshRenderer.materials;
    }

    private void Start()
    {
        RefreshStatusDisplayHUD();
    }

    /// <summary>
    /// Updates the active object selection state, showing/hiding layouts and playing context-shifted audio.
    /// </summary>
    public void ToggleElementFocus(bool stateValue)
    {
        if (isSystemTerminated) return;
        if (isCurrentlyFocused == stateValue) return;

        isCurrentlyFocused = stateValue;

        if (stateValue)
        {
            // 1. Append the Glow/Highlight pass to the existing material array
            Material[] temporaryMats = new Material[defaultMaterialCache.Length + 1];
            for (int i = 0; i < defaultMaterialCache.Length; i++)
                temporaryMats[i] = defaultMaterialCache[i];

            temporaryMats[temporaryMats.Length - 1] = selectionGlowMaterial;
            elementMeshRenderer.materials = temporaryMats;

            // 2. Manage the Floating Canvas Pop-up
            if (identityPopupPrefab != null && runtimePopupInstance == null)
            {
                Vector3 targetedSpawnPosition = transform.position + new Vector3(0f, verticalSpacialOffset, 0f);
                runtimePopupInstance = Instantiate(identityPopupPrefab, targetedSpawnPosition, Quaternion.identity, transform);

                runtimeNameLabel = runtimePopupInstance.GetComponentInChildren<TextMeshProUGUI>();
                if (runtimeNameLabel != null)
                {
                    runtimeNameLabel.text = descriptiveIdentifier;
                }
            }
            else if (runtimePopupInstance != null)
            {
                runtimePopupInstance.SetActive(true);
            }

            // 3. Play the unique sound assigned when this element is swapped into view
            if (focusAcquiredAudio != null)
            {
                AudioSource.PlayClipAtPoint(focusAcquiredAudio, transform.position);
            }

            RefreshStatusDisplayHUD();
        }
        else
        {
            // Revert materials back cleanly to default configurations
            elementMeshRenderer.materials = defaultMaterialCache;

            // Disable the layout instance instantly upon losing alignment look direction
            if (runtimePopupInstance != null)
            {
                runtimePopupInstance.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Processes incoming damage loops. Returns true if structural breakdown thresholds are broken.
    /// </summary>
    public bool ProcessIncomingImpact()
    {
        if (isSystemTerminated) return false;

        accumulatedImpacts++;
        RefreshStatusDisplayHUD();

        if (impactAudio != null && accumulatedImpacts < structuralDurability)
        {
            AudioSource.PlayClipAtPoint(impactAudio, transform.position);
        }

        if (accumulatedImpacts >= structuralDurability)
        {
            ExecuteSystemTermination();
            return true;
        }

        return false;
    }

    private void RefreshStatusDisplayHUD()
    {
        if (trackingCounterLabel != null)
        {
            trackingCounterLabel.text = $"Hits: {accumulatedImpacts}/{structuralDurability}";
        }
    }

    private void ExecuteSystemTermination()
    {
        isSystemTerminated = true;

        if (trackingCounterLabel != null)
        {
            trackingCounterLabel.text = "Hits: 0/3";
        }

        if (runtimePopupInstance != null)
        {
            Destroy(runtimePopupInstance);
        }

        ToggleElementFocus(false);

        if (terminationVFX != null)
        {
            ParticleSystem fxClone = Instantiate(terminationVFX, transform.position, Quaternion.identity);
            Destroy(fxClone.gameObject, 1f);
        }

        if (terminationAudio != null)
        {
            AudioSource.PlayClipAtPoint(terminationAudio, transform.position);
        }

        if (CameraTargetDetector.Instance != null && CameraTargetDetector.Instance.CurrentTarget == this)
        {
            CameraTargetDetector.Instance.ClearCurrentTarget();
        }

        Destroy(gameObject);
    }
}