using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Events; // Required for UnityEvents
using TMPro;

public class ARDirectPlacementDirector : MonoBehaviour
{
    [Header("AR Core Interface")]
    [SerializeField] private ARRaycastManager arRaycastComponent;

    [Header("Screen Space HUD")]
    [SerializeField] private TextMeshProUGUI trackingStatusAlert;

    [TextArea]
    [SerializeField] private string localizedSearchMsg = "Searching for floor...";
    [TextArea]
    [SerializeField] private string localizedReadyMsg = "Touch the screen to start";

    [Header("AR Events")]
    [Tooltip("This event triggers the exact frame an AR surface/floor is first detected.")]
    [SerializeField] private UnityEvent onSurfaceDetected;

    [Header("Startup Entity Visibility Filters")]
    [SerializeField] private GameObject[] initialHidingGroup;
    [SerializeField] private GameObject[] initialShowingGroup;

    [Header("Deployment Generation Configurations")]
    [Tooltip("The live scene GameObjects you want to move and animate directly on the floor where you tap.")]
    [SerializeField] private GameObject[] sourcePrefabsList;
    [Tooltip("The target custom axis scales (X, Y, Z) the GameObjects will open up into.")]
    [SerializeField] private Vector3[] targetScalesList;
    [Tooltip("Keep this array empty or use it to hold the spawned objects dynamically at runtime.")]
    [SerializeField] private Transform[] generationAnchorPoints;
    [SerializeField] private ParticleSystem generationVFX;

    [Header("Audio Output Components")]
    [SerializeField] private AudioSource mainAudioEmitter;
    [SerializeField] private AudioClip deploymentSFX;

    [Header("Scale Animation Interpolation Curves")]
    [SerializeField] private float growthTimelineSpan = 0.35f;

    private bool surfaceIsAnchored = false;
    private bool UIHasToggled = false;
    private int currentSpawnIndex = 0; // Tracks which element index to place next

    void Start()
    {
        if (trackingStatusAlert != null) trackingStatusAlert.text = localizedSearchMsg;

        foreach (GameObject entity in initialShowingGroup)
        {
            if (entity != null) entity.SetActive(false);
        }

        // Initially hide target scene objects until touch activation triggers
        foreach (GameObject entity in sourcePrefabsList)
        {
            if (entity != null) entity.SetActive(false);
        }
    }

    void Update()
    {
        if (arRaycastComponent == null) return;

        EvaluateSpatialSurfaces();

        // Block if surface isn't ready or if we have already deployed all objects in the list
        if (!surfaceIsAnchored || currentSpawnIndex >= sourcePrefabsList.Length) return;

        // Listen for screen touch input
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            // Block spawning if touching over a UI button element
            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                Vector2 touchPosition = Input.GetTouch(0).position;
                List<ARRaycastHit> structuralRayHits = new List<ARRaycastHit>();

                // Raycast against the detected physical AR plane at the touch position
                if (arRaycastComponent.Raycast(touchPosition, structuralRayHits, TrackableType.PlaneWithinPolygon))
                {
                    // Grab the precise hit placement orientation matrix
                    Pose planeHitPose = structuralRayHits[0].pose;

                    // Trigger deployment exactly where your finger tapped the floor for the current index!
                    InitiateMainOperation(planeHitPose);
                }
            }
        }
    }

    private void EvaluateSpatialSurfaces()
    {
        if (surfaceIsAnchored) return;

        Vector2 viewportCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        List<ARRaycastHit> structuralRayHits = new List<ARRaycastHit>();

        if (arRaycastComponent.Raycast(viewportCenter, structuralRayHits, TrackableType.PlaneWithinPolygon))
        {
            surfaceIsAnchored = true;
            if (trackingStatusAlert != null) trackingStatusAlert.text = localizedReadyMsg;

            // TRIGGER EVENT: Fire your custom inspector functions here!
            if (onSurfaceDetected != null)
            {
                onSurfaceDetected.Invoke();
            }
        }
    }

    private void InitiateMainOperation(Pose spawnPose)
    {
        // Only trigger UI canvas visual swaps once on the very first tap
        if (!UIHasToggled)
        {
            UIHasToggled = true;

            foreach (GameObject entity in initialHidingGroup)
            {
                if (entity != null) entity.SetActive(false);
            }

            foreach (GameObject entity in initialShowingGroup)
            {
                if (entity != null) entity.SetActive(true);
            }

            if (trackingStatusAlert != null)
                if (trackingStatusAlert.gameObject != null)
                    trackingStatusAlert.gameObject.SetActive(false);
        }

        // Deploy only the single object matching our current placement sequence index
        DeploySingleTargetStructureAtPose(spawnPose, currentSpawnIndex);

        // Increment index tracker so the next tap evaluates the next array slot item
        currentSpawnIndex++;

        if (mainAudioEmitter != null && deploymentSFX != null)
            mainAudioEmitter.PlayOneShot(deploymentSFX);
    }

    private void DeploySingleTargetStructureAtPose(Pose accurateTargetPose, int index)
    {
        if (index >= sourcePrefabsList.Length || sourcePrefabsList[index] == null) return;

        // Activate the specific scene object reference directly
        GameObject instantiatedTarget = sourcePrefabsList[index];
        instantiatedTarget.SetActive(true);

        instantiatedTarget.transform.position = accurateTargetPose.position;
        instantiatedTarget.transform.rotation = accurateTargetPose.rotation;
        instantiatedTarget.transform.localScale = Vector3.zero;

        // Fallback safety to scale cleanly to (1,1,1) if target scale array index mismatch 
        Vector3 finalScaleTarget = Vector3.one;
        if (targetScalesList != null && index < targetScalesList.Length)
        {
            finalScaleTarget = targetScalesList[index];
        }

        StartCoroutine(ExecuteDeploymentScalingAnimation(instantiatedTarget.transform, finalScaleTarget));

        // We pass 'false' to keep the original world position intact during parenting!
        if (generationAnchorPoints != null && index < generationAnchorPoints.Length && generationAnchorPoints[index] != null)
        {
            instantiatedTarget.transform.SetParent(generationAnchorPoints[index], false);

            // Explicitly re-enforce the correct touch coordinates so parenting doesn't warp it
            instantiatedTarget.transform.position = accurateTargetPose.position;
            instantiatedTarget.transform.rotation = accurateTargetPose.rotation;
        }

        if (generationVFX != null)
        {
            ParticleSystem fxInstance = Instantiate(generationVFX, accurateTargetPose.position, Quaternion.identity);
            fxInstance.Play();
            Destroy(fxInstance.gameObject, 1f);
        }
    }

    private IEnumerator ExecuteDeploymentScalingAnimation(Transform targetTransform, Vector3 maxTargetScale)
    {
        float timer = 0f;
        while (targetTransform != null && timer < growthTimelineSpan)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / growthTimelineSpan);
            progress = Mathf.SmoothStep(0f, 1f, progress);
            targetTransform.transform.localScale = Vector3.Lerp(Vector3.zero, maxTargetScale, progress);
            yield return null;
        }

        float settlingSpan = 0.12f;
        timer = 0f;
        while (targetTransform != null && timer < settlingSpan)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / settlingSpan);
            targetTransform.transform.localScale = Vector3.Lerp(maxTargetScale * 1.1f, maxTargetScale, progress);
            yield return null;
        }

        if (targetTransform != null)
        {
            targetTransform.transform.localScale = maxTargetScale;
        }
    }
}