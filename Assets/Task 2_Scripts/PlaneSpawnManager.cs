using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlaneSpawnManager : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("Spawner")]
    [SerializeField] private TargetSpawner targetSpawner;

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;

    [TextArea]
    public string searchingText = "Searching for Floor...";

    [TextArea]
    public string detectedText = "Floor Detected!\nTap anywhere to Spawn Targets.";

    private bool planeDetected;
    private bool targetsSpawned;

    private static readonly List<ARRaycastHit> hits = new();

    private void Start()
    {
        statusText.text = searchingText;
    }

    private void Update()
    {
        if (targetsSpawned)
            return;

        DetectPlane();

        if (!planeDetected)
            return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            TrySpawnTargets(Input.mousePosition);
        }
#else
        if (Input.touchCount > 0 &&
            Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TrySpawnTargets(Input.GetTouch(0).position);
        }
#endif
    }

    private void DetectPlane()
    {
        Vector2 center = new Vector2(Screen.width * .5f, Screen.height * .5f);

        bool found = raycastManager.Raycast(center, hits, TrackableType.PlaneWithinPolygon);

        if (found && !planeDetected)
        {
            planeDetected = true;
            statusText.text = detectedText;
        }
        else if (!found && planeDetected)
        {
            planeDetected = false;
            statusText.text = searchingText;
        }
    }

    private void TrySpawnTargets(Vector2 screenPosition)
    {
        if (!raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
            return;

        Pose pose = hits[0].pose;

        targetSpawner.SpawnTargets(pose.position);

        targetsSpawned = true;

        statusText.text = "";

        DisablePlaneDetection();
    }

    void DisablePlaneDetection()
    {
        ARPlaneManager planeManager = FindFirstObjectByType<ARPlaneManager>();

        if (planeManager == null)
            return;

        foreach (ARPlane plane in planeManager.trackables)
            plane.gameObject.SetActive(false);

        planeManager.enabled = false;
    }
}