using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARPartToggleManager : MonoBehaviour
{
    [Header("Director Reference Integration")]
    [Tooltip("Drag the GameObject with your new ARDirectPlacementDirector script here.")]
    [SerializeField] private ARDirectPlacementDirector placementDirector;

    [Header("Alternative Swap Assets")]
    [Tooltip("The detailed part prefabs that correspond to your original source prefabs list. Keep the list order identical!")]
    [SerializeField] private GameObject[] alternativeDetailedPrefabs;

    [Header("Animate Scaling Curves")]
    [SerializeField] private float transitionDuration = 0.25f;

    private List<GameObject> activeSwappedInstances = new List<GameObject>();
    private List<GameObject> capturedOriginalInstances = new List<GameObject>();
    private bool isShowingDetailedParts = false;

    /// <summary>
    /// Call this function from your 'PARTS' Button OnClick() event handler loop.
    /// </summary>
    public void ExecutePartSwapSwapSequence()
    {
        if (isShowingDetailedParts) return;

        // Locate what was dynamically spawned onto the touch coordinates
        CaptureSpawnedOriginalTargets();

        if (capturedOriginalInstances.Count == 0)
        {
            Debug.LogWarning("[ARPartToggleManager] No active targets detected yet. Make sure you tap the floor to spawn the object first!");
            return;
        }

        isShowingDetailedParts = true;
        ClearSwappedRegistry();

        for (int i = 0; i < capturedOriginalInstances.Count; i++)
        {
            GameObject originalObj = capturedOriginalInstances[i];
            if (originalObj == null) continue;

            // Pick the matching detailed asset index safely
            int prefabIndex = Mathf.Min(i, alternativeDetailedPrefabs.Length - 1);
            if (alternativeDetailedPrefabs[prefabIndex] == null) continue;

            // Hide the original spawned object seamlessly
            originalObj.SetActive(false);

            // Instantiate the alternative detailed piece at the exact touch position it currently sits
            GameObject detailedInstance = Instantiate(
                alternativeDetailedPrefabs[prefabIndex],
                originalObj.transform.position,
                originalObj.transform.rotation,
                originalObj.transform.parent // Keep the exact same spatial hierarchy structure
            );

            // Maintain visual scale parity
            detailedInstance.transform.localScale = originalObj.transform.localScale;
            activeSwappedInstances.Add(detailedInstance);

            StartCoroutine(ExecuteScalingTransition(detailedInstance.transform, Vector3.zero, originalObj.transform.localScale));
        }
    }

    /// <summary>
    /// Call this function from your 'BACK' Button OnClick() event handler loop.
    /// </summary>
    public void ExecuteReverseReversionSequence()
    {
        if (!isShowingDetailedParts) return;

        isShowingDetailedParts = false;

        // Restore visibility back to all original spawned elements
        for (int i = 0; i < capturedOriginalInstances.Count; i++)
        {
            if (capturedOriginalInstances[i] != null)
            {
                capturedOriginalInstances[i].SetActive(true);
                Vector3 targetScale = capturedOriginalInstances[i].transform.localScale;
                StartCoroutine(ExecuteScalingTransition(capturedOriginalInstances[i].transform, Vector3.zero, targetScale));
            }
        }

        ClearSwappedRegistry();
    }

    private void CaptureSpawnedOriginalTargets()
    {
        capturedOriginalInstances.Clear();

        if (placementDirector == null)
        {
            placementDirector = Object.FindFirstObjectByType<ARDirectPlacementDirector>();

            if (placementDirector == null)
            {
                Debug.LogError("[ARPartToggleManager] ARDirectPlacementDirector reference is missing! Please assign it in the inspector.");
                return;
            }
        }

        // FIX: Search by Transform instead of Collider. This detects any object regardless of physics components.
        Transform[] allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Transform t in allTransforms)
        {
            GameObject go = t.gameObject;

            // Check if the object is a clone spawned by the director
            if (go.name.Contains("(Clone)") && !go.name.Contains("Bullet") && !go.name.Contains("Canvas"))
            {
                // Find the top-most root parent of this clone to prevent grabbing interior child pieces
                Transform rootClone = t;
                while (rootClone.parent != null && rootClone.parent.name.Contains("(Clone)"))
                {
                    rootClone = rootClone.parent;
                }

                if (!capturedOriginalInstances.Contains(rootClone.gameObject))
                {
                    capturedOriginalInstances.Add(rootClone.gameObject);
                }
            }
        }
    }

    private void ClearSwappedRegistry()
    {
        foreach (GameObject obj in activeSwappedInstances)
        {
            if (obj != null) Destroy(obj);
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
            targetTransform.localScale = Vector3.Lerp(fromScale, toScale, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        if (targetTransform != null) targetTransform.localScale = toScale;
    }

    private void OnDestroy()
    {
        ClearSwappedRegistry();
    }
}