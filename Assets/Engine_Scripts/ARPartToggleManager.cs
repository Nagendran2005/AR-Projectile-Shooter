using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARPartToggleManager : MonoBehaviour
{
    [Header("Director Reference Integration")]
    [Tooltip("Drag the GameObject with your ARLiquidGameDirector script here.")]
    [SerializeField] private ARLiquidGameDirector gameDirector;

    [Header("Alternative Swap Assets")]
    [Tooltip("The detailed part prefabs that correspond to your original source prefabs list. Keep the list order identical!")]
    [SerializeField] private GameObject[] alternativeDetailedPrefabs;

    [Header("Animate Scaling Curves")]
    [SerializeField] private float transitionDuration = 0.25f;

    // Track the runtime instantiated instances
    private List<GameObject> activeSwappedInstances = new List<GameObject>();
    private List<GameObject> capturedOriginalInstances = new List<GameObject>();
    private bool isShowingDetailedParts = false;

    /// <summary>
    /// Call this function from your 'PARTS' Button OnClick() event handler loop.
    /// </summary>
    public void ExecutePartSwapSwapSequence()
    {
        if (isShowingDetailedParts) return; // Prevent double execution if already showing detailed parts

        // Find all active capsule objects spawned in the scene dynamically
        CaptureSpawnedOriginalTargets();

        if (capturedOriginalInstances.Count == 0)
        {
            Debug.LogWarning("[ARPartToggleManager] No spawned active original targets detected in the scene to swap.");
            return;
        }

        isShowingDetailedParts = true;

        // Clear out any old swapped items safely if they exist
        ClearSwappedRegistry();

        for (int i = 0; i < capturedOriginalInstances.Count; i++)
        {
            GameObject originalObj = capturedOriginalInstances[i];
            if (originalObj == null) continue;

            // Pick the matching detailed asset index framework (fallback to index 0 if array bounds mismatch)
            int prefabIndex = Mathf.Min(i, alternativeDetailedPrefabs.Length - 1);
            if (alternativeDetailedPrefabs[prefabIndex] == null) continue;

            // Hide the original spawned object seamlessly
            originalObj.SetActive(false);

            // Instantiate the alternative detailed piece at the exact same location coordinates
            GameObject detailedInstance = Instantiate(
                alternativeDetailedPrefabs[prefabIndex],
                originalObj.transform.position,
                originalObj.transform.rotation
            );

            // Mirror the current master hierarchy scale
            detailedInstance.transform.localScale = originalObj.transform.localScale;
            activeSwappedInstances.Add(detailedInstance);

            // Optional structural presentation pop juice
            StartCoroutine(ExecuteScalingTransition(detailedInstance.transform, Vector3.zero, originalObj.transform.localScale));
        }
    }

    /// <summary>
    /// Call this function from your 'BACK' Button OnClick() event handler loop to reverse the swap.
    /// </summary>
    public void ExecuteReverseReversionSequence()
    {
        if (!isShowingDetailedParts) return;

        isShowingDetailedParts = false;

        // Restore visibility back to all original spawned parts tracking elements
        for (int i = 0; i < capturedOriginalInstances.Count; i++)
        {
            if (capturedOriginalInstances[i] != null)
            {
                capturedOriginalInstances[i].SetActive(true);

                // Pop the original back into view smoothly
                Vector3 targetScale = capturedOriginalInstances[i].transform.localScale;
                StartCoroutine(ExecuteScalingTransition(capturedOriginalInstances[i].transform, Vector3.zero, targetScale));
            }
        }

        // Wipe out the temporary alternative modular display pieces safely
        ClearSwappedRegistry();
    }

    private void CaptureSpawnedOriginalTargets()
    {
        capturedOriginalInstances.Clear();

        // Dynamically locate any active target elements matching the template signature in the scene
        ARLiquidCapsule[] objectsInScene = FindObjectsByType<ARLiquidCapsule>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (ARLiquidCapsule capsule in objectsInScene)
        {
            if (capsule != null)
            {
                capturedOriginalInstances.Add(capsule.gameObject);
            }
        }
    }

    private void ClearSwappedRegistry()
    {
        foreach (GameObject obj in activeSwappedInstances)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        activeSwappedInstances.Clear();
    }

    private IEnumerator ExecuteScalingTransition(Transform targetTransform, Vector3 fromScale, Vector3 toScale)
    {
        float timer = 0f;
        targetTransform.localScale = fromScale;

        while (timer < transitionDuration)
        {
            if (targetTransform == null) yield break;

            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / transitionDuration);
            progress = Mathf.SmoothStep(0f, 1f, progress); // Smooth easing curve

            targetTransform.localScale = Vector3.Lerp(fromScale, toScale, progress);
            yield return null;
        }

        if (targetTransform != null) targetTransform.localScale = toScale;
    }

    private void OnDestroy()
    {
        ClearSwappedRegistry();
    }
}