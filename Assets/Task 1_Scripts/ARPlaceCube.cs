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
            PlaceObject(Input.GetTouch(0).position);
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
            statusText.text = detectedMessage;
        }
        else if (!foundPlane && planeDetected)
        {
            planeDetected = false;
            statusText.text = detectingMessage;
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

            // Start with scale 0
            obj.transform.localScale = Vector3.zero;

            // Animate scale
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

            statusText.text = detectedMessage;
        }
    }

    IEnumerator ScaleAnimation(Transform target)
    {
        float timer = 0f;

        while (timer < spawnDuration)
        {
            timer += Time.deltaTime;

            float t = timer / spawnDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            float scale = Mathf.Lerp(0f, 1.15f, t);

            target.localScale = Vector3.one * scale;

            yield return null;
        }

        float bounceTime = 0.1f;
        timer = 0f;

        while (timer < bounceTime)
        {
            timer += Time.deltaTime;

            float t = timer / bounceTime;

            target.localScale = Vector3.Lerp(
                Vector3.one * 0.3f,
                Vector3.one,
                t);

            yield return null;
        }

        target.localScale = Vector3.one;
    }
}