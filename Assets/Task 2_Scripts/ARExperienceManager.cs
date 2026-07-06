using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class ARExperienceManager : MonoBehaviour
{
    [Header("AR Framework")]
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("UI Text Displays")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI hitCountText; // Drag your 'Distroy count' scene UI element here!

    [TextArea]
    [SerializeField] private string searchingMessage = "Searching for floor...";
    [TextArea]
    [SerializeField] private string readyMessage = "Touch the screen to start";

    [Header("Objects To Hide on Start")]
    [SerializeField] private GameObject[] objectsToHide;

    [Header("Objects To Show on Start")]
    [SerializeField] private GameObject[] objectsToShow;

    [Header("NEW: Wave Clear Transitions")]
    [SerializeField] private GameObject[] objectsToHideOnClear; // Objects to hide when all targets die
    [SerializeField] private GameObject[] objectsToShowOnClear; // Objects to show when all targets die

    [Header("Prefabs To Spawn (Size = 3)")]
    [SerializeField] private GameObject[] prefabs;

    [Header("Spawn Slots (Size = 3)")]
    [SerializeField] private Transform[] spawnSlots;

    [Header("Effects")]
    [SerializeField] private ParticleSystem spawnEffect;

    [Header("Audio Elements")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip spawnSFX;
    [SerializeField] private AudioClip shootSFX;

    [Header("Camera Shooting & Pooling Configuration")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private float maxShootDistance = 60f;
    [SerializeField] private int poolSize = 12;

    [Header("Animation Curves")]
    [SerializeField] private float spawnDuration = 0.35f;

    private bool isPlaneDetected = false;
    private bool hasStartedExperience = false;
    private int currentScore = 0;

    // --- GLOBAL HIT CALCULATOR FIELDS ---
    private TargetController currentTrackedTarget = null;
    private int globalHitCount = 0;
    private const int MAX_HITS_PER_TARGET = 3;

    // --- NEW: TARGET TRACKING ---
    private int remainingTargetsCount = 0;

    private Queue<GameObject> bulletPool = new Queue<GameObject>();

    void Start()
    {
        if (statusText != null) statusText.text = searchingMessage;
        if (scoreText != null) scoreText.text = "Score: 0";

        ResetGlobalHitCount();

        foreach (GameObject obj in objectsToShow)
        {
            if (obj != null) obj.SetActive(false);
        }

        // Ensure wave clear objects are in their default state at start
        foreach (GameObject obj in objectsToShowOnClear)
        {
            if (obj != null) obj.SetActive(false);
        }

        InitializeBulletPool();
    }

    void Update()
    {
        if (raycastManager == null) return;

        DetectSurface();

        if (!isPlaneDetected || hasStartedExperience) return;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                StartARExperience();
            }
        }
    }

    private void DetectSurface()
    {
        if (isPlaneDetected) return;

        Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
        List<ARRaycastHit> hits = new List<ARRaycastHit>();

        if (raycastManager.Raycast(center, hits, TrackableType.PlaneWithinPolygon))
        {
            isPlaneDetected = true;
            if (statusText != null) statusText.text = readyMessage;
        }
    }

    private void StartARExperience()
    {
        hasStartedExperience = true;

        foreach (GameObject obj in objectsToHide)
        {
            if (obj != null) obj.SetActive(false);
        }

        foreach (GameObject obj in objectsToShow)
        {
            if (obj != null) obj.SetActive(true);
        }

        SpawnPrefabs();

        if (audioSource != null && spawnSFX != null)
            audioSource.PlayOneShot(spawnSFX);

        if (statusText != null)
            statusText.gameObject.SetActive(false);
    }

    private void SpawnPrefabs()
    {
        int count = Mathf.Min(prefabs.Length, spawnSlots.Length);
        remainingTargetsCount = count; // Set tracking to total spawned objects

        for (int i = 0; i < count; i++)
        {
            if (prefabs[i] == null || spawnSlots[i] == null) continue;

            GameObject obj = Instantiate(prefabs[i], spawnSlots[i].position, spawnSlots[i].rotation);
            obj.transform.localScale = Vector3.zero;

            TargetController targetScript = obj.GetComponent<TargetController>();
            if (targetScript == null)
            {
                targetScript = obj.GetComponentInParent<TargetController>();
            }

            if (targetScript != null)
            {
                targetScript.hitCountText = hitCountText;
            }

            StartCoroutine(AnimateSpawnScale(obj.transform));

            if (spawnEffect != null)
            {
                ParticleSystem effect = Instantiate(spawnEffect, spawnSlots[i].position, Quaternion.identity);
                effect.Play();
                Destroy(effect.gameObject, 1f);
            }
        }
    }

    private IEnumerator AnimateSpawnScale(Transform target)
    {
        float timer = 0f;
        while (timer < spawnDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / spawnDuration);
            t = Mathf.SmoothStep(0f, 1f, t);
            target.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            yield return null;
        }

        float bounceTime = 0.12f;
        timer = 0f;
        while (timer < bounceTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / bounceTime);
            target.transform.localScale = Vector3.Lerp(Vector3.one * 1.1f, Vector3.one, t);
            yield return null;
        }
        target.transform.localScale = Vector3.one;
    }

    private void InitializeBulletPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);
            bulletPool.Enqueue(bullet);
        }
    }

    // --- ASSIGN TO YOUR 'FIRE' UI BUTTON ---
    public void FireBullet()
    {
        if (!hasStartedExperience || bulletSpawnPoint == null) return;

        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Ray ray = mainCam.ScreenPointToRay(screenCenter);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxShootDistance, targetLayer))
        {
            TargetController target = hit.collider.GetComponent<TargetController>();
            if (target == null)
            {
                target = hit.collider.GetComponentInParent<TargetController>();
            }

            if (target != null)
            {
                if (currentTrackedTarget != target)
                {
                    currentTrackedTarget = target;
                    globalHitCount = 0;
                }

                globalHitCount++;
                UpdateHitCountUI();

                bool destroyed = target.TakeHit();
                if (destroyed)
                {
                    AddScore(10);
                    ResetGlobalHitCount();

                    // NEW: Track remaining targets
                    remainingTargetsCount--;
                    if (remainingTargetsCount <= 0)
                    {
                        OnAllTargetsDestroyed();
                    }
                }
            }
        }

        GameObject bullet = GetBulletFromPool();
        if (bullet != null)
        {
            ARBullet bulletScript = bullet.GetComponent<ARBullet>();
            if (bulletScript != null)
            {
                Quaternion bulletRotation = Quaternion.LookRotation(ray.direction);
                if (hit.collider != null)
                {
                    bulletRotation = Quaternion.LookRotation(hit.point - bulletSpawnPoint.position);
                }

                bulletScript.Launch(bulletSpawnPoint.position, bulletRotation, ReturnBulletToPool);
            }
        }

        if (audioSource != null && shootSFX != null)
            audioSource.PlayOneShot(shootSFX);
    }

    // --- NEW: WAVE CLEAR SEQUENCE ---
    private void OnAllTargetsDestroyed()
    {
        // Hide specified objects (e.g., Gun, fire button, target counters)
        foreach (GameObject obj in objectsToHideOnClear)
        {
            if (obj != null) obj.SetActive(false);
        }

        // Show specified objects (e.g., your green "WAVE CLEARED" banner layout)
        foreach (GameObject obj in objectsToShowOnClear)
        {
            if (obj != null) obj.SetActive(true);
        }
    }

    private void UpdateHitCountUI()
    {
        if (hitCountText != null)
        {
            hitCountText.text = $"Hits: {globalHitCount}/{MAX_HITS_PER_TARGET}";
        }
    }

    private void ResetGlobalHitCount()
    {
        globalHitCount = 0;
        currentTrackedTarget = null;
        if (hitCountText != null)
        {
            hitCountText.text = "Hits: 0/3";
        }
    }

    private GameObject GetBulletFromPool()
    {
        if (bulletPool.Count > 0) return bulletPool.Dequeue();
        GameObject bullet = Instantiate(bulletPrefab);
        return bullet;
    }

    private void ReturnBulletToPool(GameObject bullet)
    {
        bulletPool.Enqueue(bullet);
    }

    public void AddScore(int points)
    {
        currentScore += points;
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore;
        }
    }
}