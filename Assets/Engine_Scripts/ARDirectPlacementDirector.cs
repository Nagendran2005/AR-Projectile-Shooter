using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
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

    [Header("Startup Entity Visibility Filters")]
    [SerializeField] private GameObject[] initialHidingGroup;
    [SerializeField] private GameObject[] initialShowingGroup;

    [Header("Deployment Generation Configurations")]
    [Tooltip("The prefabs you want to spawn directly on the floor where you tap.")]
    [SerializeField] private GameObject[] sourcePrefabsList;
    [Tooltip("Keep this array empty or use it to hold the spawned objects dynamically at runtime.")]
    [SerializeField] private Transform[] generationAnchorPoints;
    [SerializeField] private ParticleSystem generationVFX;

    [Header("Audio Output Components")]
    [SerializeField] private AudioSource mainAudioEmitter;
    [SerializeField] private AudioClip deploymentSFX;

    [Header("Scale Animation Interpolation Curves")]
    [SerializeField] private float growthTimelineSpan = 0.35f;

    private bool surfaceIsAnchored = false;
    private bool operationIsRunning = false;
    private int remainingLiveTargets = 0;

    void Start()
    {
        if (trackingStatusAlert != null) trackingStatusAlert.text = localizedSearchMsg;

        foreach (GameObject entity in initialShowingGroup)
        {
            if (entity != null) entity.SetActive(false);
        }
    }

    void Update()
    {
        if (arRaycastComponent == null) return;

        EvaluateSpatialSurfaces();

        if (!surfaceIsAnchored || operationIsRunning) return;

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

                    // Trigger deployment exactly where your finger tapped the floor!
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
        }
    }

    private void InitiateMainOperation(Pose spawnPose)
    {
        operationIsRunning = true;

        foreach (GameObject entity in initialHidingGroup)
        {
            if (entity != null) entity.SetActive(false);
        }

        foreach (GameObject entity in initialShowingGroup)
        {
            if (entity != null) entity.SetActive(true);
        }

        // Deploy the structures directly at the calculated touch surface coordinates
        DeployTargetStructuresAtPose(spawnPose);

        if (mainAudioEmitter != null && deploymentSFX != null)
            mainAudioEmitter.PlayOneShot(deploymentSFX);

        if (trackingStatusAlert != null)
            trackingStatusAlert.gameObject.SetActive(false);
    }

    private void DeployTargetStructuresAtPose(Pose accurateTargetPose)
    {
        // Spawns based on the prefabs assigned in your source list
        int totalToSpawn = sourcePrefabsList.Length;
        remainingLiveTargets = totalToSpawn;

        for (int i = 0; i < totalToSpawn; i++)
        {
            if (sourcePrefabsList[i] == null) continue;

            // Spawn directly at the touch screen intersection matrix position
            GameObject instantiatedTarget = Instantiate(sourcePrefabsList[i], accurateTargetPose.position, accurateTargetPose.rotation);
            instantiatedTarget.transform.localScale = Vector3.zero;

            // Run your original smooth scaling entry sequence
            StartCoroutine(ExecuteDeploymentScalingAnimation(instantiatedTarget.transform));

            // If you have a scene anchor assigned manually, parent it so it stays organized
            if (generationAnchorPoints != null && i < generationAnchorPoints.Length && generationAnchorPoints[i] != null)
            {
                instantiatedTarget.transform.SetParent(generationAnchorPoints[i]);
            }

            if (generationVFX != null)
            {
                ParticleSystem fxInstance = Instantiate(generationVFX, accurateTargetPose.position, Quaternion.identity);
                fxInstance.Play();
                Destroy(fxInstance.gameObject, 1f);
            }
        }
    }

    private IEnumerator ExecuteDeploymentScalingAnimation(Transform targetTransform)
    {
        float timer = 0f;
        while (timer < growthTimelineSpan)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / growthTimelineSpan);
            progress = Mathf.SmoothStep(0f, 1f, progress);
            targetTransform.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, progress);
            yield return null;
        }

        float settlingSpan = 0.12f;
        timer = 0f;
        while (timer < settlingSpan)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / settlingSpan);
            targetTransform.transform.localScale = Vector3.Lerp(Vector3.one * 1.1f, Vector3.one, progress);
            yield return null;
        }
        targetTransform.transform.localScale = Vector3.one;
    }
}