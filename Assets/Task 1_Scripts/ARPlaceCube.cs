using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class ARPlaceCube : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("Prefab")]
    [SerializeField] private GameObject objectPrefab;

    [Header("Custom Inspector Scaling Controls")]
    [Tooltip("The final target local scale your prefab should reach once spawned.")]
    [SerializeField] private Vector3 targetFinalScale = Vector3.one;

    [Tooltip("The temporary maximum scale multiplier during the pop animation (e.g., 1.15 means it overshoots by 15%).")]
    [SerializeField] private float popScaleMultiplier = 1.15f;

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;

    [TextArea]
    public string detectingMessage = "Searching for floor...";

    [TextArea]
    public string detectedMessage = "Plane Detected!\nTap anywhere to place.";

    [Header("Placement Effect")]
    [SerializeField] private ParticleSystem placeEffect;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip placeSFX;

    [Header("Animation")]
    [SerializeField] private float spawnDuration = 0.3f;

    private bool planeDetected = false;

    void Start()
    {
        if (statusText != null)
            statusText.text = detectingMessage;
    }

    void Update()
    {
        if (raycastManager == null)
            return;

        CheckPlaneDetection();

        if (!planeDetected)
            return;

        if (Input.touchCount > 0 &&
            Input.GetTouch(0).phase == TouchPhase.Began)
        {
            // Block placement if a user is clicking overlay UI buttons
            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                PlaceObject(Input.GetTouch(0).position);
            }
        }
    }

    void CheckPlaneDetection()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        List<ARRaycastHit> hits = new List<ARRaycastHit>();

        bool foundPlane = raycastManager.Raycast(
            screenCenter,
            hits,
            TrackableType.PlaneWithinPolygon);

        if (foundPlane && !planeDetected)
        {
            planeDetected = true;
            if (statusText != null) statusText.text = detectedMessage;
        }
        else if (!foundPlane && planeDetected)
        {
            planeDetected = false;
            if (statusText != null) statusText.text = detectingMessage;
        }
    }

    void PlaceObject(Vector2 touchPosition)
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();

        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose pose = hits[0].pose;

            // Spawn prefab
            GameObject obj = Instantiate(objectPrefab, pose.position, pose.rotation);

            // Start completely invisible at 0
            obj.transform.localScale = Vector3.zero;

            // Animate scale dynamically using inspector parameters
            StartCoroutine(ScaleAnimation(obj.transform));

            // Play SFX
            if (audioSource != null && placeSFX != null)
            {
                audioSource.PlayOneShot(placeSFX);
            }

            // Spawn particle
            if (placeEffect != null)
            {
                ParticleSystem effect = Instantiate(
                    placeEffect,
                    pose.position,
                    Quaternion.identity);

                effect.Play();
                Destroy(effect.gameObject, 1f);
            }

            if (statusText != null) statusText.text = detectedMessage;
        }
    }

    IEnumerator ScaleAnimation(Transform target)
    {
        float timer = 0f;
        Vector3 peakScale = targetFinalScale * popScaleMultiplier;

        // Phase 1: Grow out from zero to the overshooting peak size
        while (timer < spawnDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / spawnDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            target.localScale = Vector3.Lerp(Vector3.zero, peakScale, t);

            yield return null;
        }

        // Phase 2: Settle down from the peak back down smoothly to the desired final scale
        float bounceTime = 0.1f;
        timer = 0f;

        while (timer < bounceTime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / bounceTime);

            target.localScale = Vector3.Lerp(peakScale, targetFinalScale, t);

            yield return null;
        }

        // Lock perfectly into the user's Inspector scale settings
        target.localScale = targetFinalScale;
    }
}