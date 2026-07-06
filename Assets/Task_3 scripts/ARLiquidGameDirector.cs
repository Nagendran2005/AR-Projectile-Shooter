using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class ARLiquidGameDirector : MonoBehaviour
{
    [Header("AR Core Interface")]
    [SerializeField] private ARRaycastManager arRaycastComponent;

    [Header("Screen Space HUD")]
    [SerializeField] private TextMeshProUGUI trackingStatusAlert;
    [SerializeField] private TextMeshProUGUI aggregateScoreDisplay;
    [SerializeField] private TextMeshProUGUI capsuleStrikeCounter;

    [TextArea]
    [SerializeField] private string localizedSearchMsg = "Searching for floor...";
    [TextArea]
    [SerializeField] private string localizedReadyMsg = "Touch the screen to start";

    [Header("Startup Entity Visibility Filters")]
    [SerializeField] private GameObject[] initialHidingGroup;
    [SerializeField] private GameObject[] initialShowingGroup;

    [Header("Phase Cleanse Visibility Transitions")]
    [SerializeField] private GameObject[] phaseClearHidingGroup;
    [SerializeField] private GameObject[] phaseClearShowingGroup;

    [Header("Deployment Generation Configurations")]
    [SerializeField] private GameObject[] sourcePrefabsList;
    [SerializeField] private Transform[] generationAnchorPoints;
    [SerializeField] private ParticleSystem generationVFX;

    [Header("Audio Output Components")]
    [SerializeField] private AudioSource mainAudioEmitter;
    [SerializeField] private AudioClip deploymentSFX;
    [SerializeField] private AudioClip projectileLaunchSFX;

    [Header("Projectile Pooling Parameters")]
    [SerializeField] private GameObject poolingBulletPrefab;
    [SerializeField] private Transform projectileMuzzleNode;
    [SerializeField] private LayerMask targetedLayerFilter;
    [SerializeField] private float absoluteRaycastRange = 60f;
    [SerializeField] private int initialPoolCapacity = 12;

    [Header("Scale Animation Interpolation Curves")]
    [SerializeField] private float growthTimelineSpan = 0.35f;

    private bool surfaceIsAnchored = false;
    private bool operationIsRunning = false;
    private int internalScoreTally = 0;

    private ARLiquidCapsule targetCurrentlyLocked = null;
    private int targetedCapsuleStrikes = 0;
    private const int STRIKES_LIMIT_PER_TARGET = 3;
    private int remainingLiveTargets = 0;

    private Queue<GameObject> structuralBulletPool = new Queue<GameObject>();

    void Start()
    {
        if (trackingStatusAlert != null) trackingStatusAlert.text = localizedSearchMsg;
        if (aggregateScoreDisplay != null) aggregateScoreDisplay.text = "Score: 0";

        ClearGlobalStrikeMetrics();

        foreach (GameObject entity in initialShowingGroup)
        {
            if (entity != null) entity.SetActive(false);
        }

        foreach (GameObject entity in phaseClearShowingGroup)
        {
            if (entity != null) entity.SetActive(false);
        }

        AssembleProjectileCache();
    }

    void Update()
    {
        if (arRaycastComponent == null) return;

        EvaluateSpatialSurfaces();

        if (!surfaceIsAnchored || operationIsRunning) return;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                InitiateMainOperation();
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

    private void InitiateMainOperation()
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

        DeployTargetStructures();

        if (mainAudioEmitter != null && deploymentSFX != null)
            mainAudioEmitter.PlayOneShot(deploymentSFX);

        if (trackingStatusAlert != null)
            trackingStatusAlert.gameObject.SetActive(false);
    }

    private void DeployTargetStructures()
    {
        int totalToSpawn = Mathf.Min(sourcePrefabsList.Length, generationAnchorPoints.Length);
        remainingLiveTargets = totalToSpawn;

        for (int i = 0; i < totalToSpawn; i++)
        {
            if (sourcePrefabsList[i] == null || generationAnchorPoints[i] == null) continue;

            GameObject instantiatedTarget = Instantiate(sourcePrefabsList[i], generationAnchorPoints[i].position, generationAnchorPoints[i].rotation);
            instantiatedTarget.transform.localScale = Vector3.zero;

            StartCoroutine(ExecuteDeploymentScalingAnimation(instantiatedTarget.transform));

            if (generationVFX != null)
            {
                ParticleSystem fxInstance = Instantiate(generationVFX, generationAnchorPoints[i].position, Quaternion.identity);
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

    private void AssembleProjectileCache()
    {
        for (int i = 0; i < initialPoolCapacity; i++)
        {
            GameObject cachedBullet = Instantiate(poolingBulletPrefab);
            cachedBullet.SetActive(false);
            structuralBulletPool.Enqueue(cachedBullet);
        }
    }

    public void ExecuteWeaponDischarge()
    {
        if (!operationIsRunning || projectileMuzzleNode == null) return;

        Camera viewCamera = Camera.main;
        if (viewCamera == null) return;

        Vector3 screenViewportMidpoint = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Ray projectionRay = viewCamera.ScreenPointToRay(screenViewportMidpoint);
        RaycastHit geometryHitInfo;

        if (Physics.Raycast(projectionRay, out geometryHitInfo, absoluteRaycastRange, targetedLayerFilter))
        {
            ARLiquidCapsule targetCapsule = geometryHitInfo.collider.GetComponent<ARLiquidCapsule>();
            if (targetCapsule == null)
            {
                targetCapsule = geometryHitInfo.collider.GetComponentInParent<ARLiquidCapsule>();
            }

            if (targetCapsule != null)
            {
                if (targetCurrentlyLocked != targetCapsule)
                {
                    targetCurrentlyLocked = targetCapsule;
                    targetedCapsuleStrikes = 0;
                }

                targetedCapsuleStrikes++;
                RefreshStrikeDisplayHUD();

                bool assetWasRuptured = targetCapsule.RegisterImpact();
                if (assetWasRuptured)
                {
                    IncrementPlayerScore(10);
                    ClearGlobalStrikeMetrics();

                    remainingLiveTargets--;
                    if (remainingLiveTargets <= 0)
                    {
                        ProcessPhaseClearSequence();
                    }
                }
            }
        }

        GameObject visualBulletInstance = ExtractBulletFromCache();
        if (visualBulletInstance != null)
        {
            ARBullet visualScript = visualBulletInstance.GetComponent<ARBullet>();
            if (visualScript != null)
            {
                Quaternion operationalRotation = Quaternion.LookRotation(projectionRay.direction);
                if (geometryHitInfo.collider != null)
                {
                    operationalRotation = Quaternion.LookRotation(geometryHitInfo.point - projectileMuzzleNode.position);
                }

                visualScript.Launch(projectileMuzzleNode.position, operationalRotation, SafeRecycleToCache);
            }
        }

        if (mainAudioEmitter != null && projectileLaunchSFX != null)
            mainAudioEmitter.PlayOneShot(projectileLaunchSFX);
    }

    private void ProcessPhaseClearSequence()
    {
        foreach (GameObject entity in phaseClearHidingGroup)
        {
            if (entity != null) entity.SetActive(false);
        }

        foreach (GameObject entity in phaseClearShowingGroup)
        {
            if (entity != null) entity.SetActive(true);
        }
    }

    private void RefreshStrikeDisplayHUD()
    {
        if (capsuleStrikeCounter != null)
        {
            capsuleStrikeCounter.text = $"Hits: {targetedCapsuleStrikes}/{STRIKES_LIMIT_PER_TARGET}";
        }
    }

    private void ClearGlobalStrikeMetrics()
    {
        targetedCapsuleStrikes = 0;
        targetCurrentlyLocked = null;
        if (capsuleStrikeCounter != null)
        {
            capsuleStrikeCounter.text = "Hits: 0/3";
        }
    }

    private GameObject ExtractBulletFromCache()
    {
        if (structuralBulletPool.Count > 0) return structuralBulletPool.Dequeue();
        return Instantiate(poolingBulletPrefab);
    }

    private void SafeRecycleToCache(GameObject activeBulletEntity)
    {
        structuralBulletPool.Enqueue(activeBulletEntity);
    }

    public void IncrementPlayerScore(int scoreValueDelta)
    {
        internalScoreTally += scoreValueDelta;
        if (aggregateScoreDisplay != null)
        {
            aggregateScoreDisplay.text = "Score: " + internalScoreTally;
        }
    }
}