using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineExplosionManager : MonoBehaviour
{
    // A clean structure defining configuration values per part item
    [System.Serializable]
    public struct SystemPartConfig
    {
        [Tooltip("The name label helper just to organize your list in the Inspector.")]
        public string partIdentifierName;

        [Tooltip("Drag the target object sub-mesh or assembly node component here.")]
        public Transform targetTransform;

        [Tooltip("The destination offset or point relative to the local parent space.")]
        public Vector3 explodedLocalPosition;

        [Tooltip("The target Euler angles for rotation (Axis-Wise: X, Y, Z).")]
        public Vector3 explodedLocalRotation;

        [Tooltip("Set a speed (e.g., 50) to make this specific object spin continuously around its Y-axis AFTER exploding. Set to 0 to disable.")]
        public float continuousRotationSpeed;
    }

    [Header("Exploded Assembly Matrix")]
    [SerializeField] private SystemPartConfig[] enginePartsList;

    [Header("Animation Curves Tuning")]
    [SerializeField] private float transitionDuration = 1.5f;

    // Interior cached data collections to store initial transform values dynamically
    private Vector3[] defaultLocalPositions;
    private Quaternion[] defaultLocalRotations;

    private Coroutine activeTransitionRoutine;
    private bool isCurrentlyExploded = false;
    private bool shouldSpinActiveParts = false;

    private void Awake()
    {
        if (enginePartsList == null || enginePartsList.Length == 0)
        {
            Debug.LogError($"[EngineExplosionManager] Please populate the parts matrix parameters on {gameObject.name}!", this);
            return;
        }

        // Initialize state storage collections arrays
        defaultLocalPositions = new Vector3[enginePartsList.Length];
        defaultLocalRotations = new Quaternion[enginePartsList.Length];

        // Store baseline initial settings dynamically upon startup
        for (int i = 0; i < enginePartsList.Length; i++)
        {
            if (enginePartsList[i].targetTransform != null)
            {
                defaultLocalPositions[i] = enginePartsList[i].targetTransform.localPosition;
                defaultLocalRotations[i] = enginePartsList[i].targetTransform.localRotation;
            }
        }
    }

    private void Update()
    {
        // Continuous rotation module loop executing only when fully exploded
        if (shouldSpinActiveParts && isCurrentlyExploded)
        {
            for (int i = 0; i < enginePartsList.Length; i++)
            {
                if (enginePartsList[i].targetTransform != null && enginePartsList[i].continuousRotationSpeed != 0f)
                {
                    // Rotates the object dynamically over time on its local Y-axis based on specified speed
                    enginePartsList[i].targetTransform.Rotate(Vector3.up, enginePartsList[i].continuousRotationSpeed * Time.deltaTime, Space.Self);
                }
            }
        }
    }

    /// <summary>
    /// Call this function from your EXPLORE Button OnClick() event handler loop.
    /// </summary>
    public void TriggerExplodedView()
    {
        if (isCurrentlyExploded) return;

        if (activeTransitionRoutine != null) StopCoroutine(activeTransitionRoutine);
        shouldSpinActiveParts = false; // Turn off spinning during transition calculations
        activeTransitionRoutine = StartCoroutine(AnimateEngineLayout(true));
    }

    /// <summary>
    /// Call this function from your RESET/BACK Button OnClick() event handler loop.
    /// </summary>
    public void TriggerReassemblyView()
    {
        if (!isCurrentlyExploded) return;

        if (activeTransitionRoutine != null) StopCoroutine(activeTransitionRoutine);
        shouldSpinActiveParts = false; // Kill spinning instantly when returning home
        activeTransitionRoutine = StartCoroutine(AnimateEngineLayout(false));
    }

    private IEnumerator AnimateEngineLayout(bool toExplodedState)
    {
        float elapsed = 0f;
        isCurrentlyExploded = toExplodedState;

        // Create temporary snapshot arrays to handle blending operations smoothly
        Vector3[] startPositions = new Vector3[enginePartsList.Length];
        Quaternion[] startRotations = new Quaternion[enginePartsList.Length];

        // Capture present configuration values the moment transition is called
        for (int i = 0; i < enginePartsList.Length; i++)
        {
            if (enginePartsList[i].targetTransform != null)
            {
                startPositions[i] = enginePartsList[i].targetTransform.localPosition;
                startRotations[i] = enginePartsList[i].targetTransform.localRotation;
            }
        }

        // Animation progress loop tracking layout states
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float progressRatio = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothlyEasedProgress = Mathf.SmoothStep(0f, 1f, progressRatio);

            for (int i = 0; i < enginePartsList.Length; i++)
            {
                Transform currentPart = enginePartsList[i].targetTransform;
                if (currentPart == null) continue;

                // Determine target parameters based on chosen state direction
                Vector3 finalPos = toExplodedState ? enginePartsList[i].explodedLocalPosition : defaultLocalPositions[i];
                Quaternion finalRot = toExplodedState ? Quaternion.Euler(enginePartsList[i].explodedLocalRotation) : defaultLocalRotations[i];

                // Linearly interpolate positions and rotations step by step
                currentPart.localPosition = Vector3.Lerp(startPositions[i], finalPos, smoothlyEasedProgress);
                currentPart.localRotation = Quaternion.Slerp(startRotations[i], finalRot, smoothlyEasedProgress);
            }

            yield return null;
        }

        // Final enforcement snap pass to ensure structural alignment targets are perfectly met
        SnapToFinalState(toExplodedState);

        // Turn continuous spinning back on safely if we just finished expanding
        if (toExplodedState)
        {
            shouldSpinActiveParts = true;
        }
    }

    private void SnapToFinalState(bool toExplodedState)
    {
        for (int i = 0; i < enginePartsList.Length; i++)
        {
            Transform currentPart = enginePartsList[i].targetTransform;
            if (currentPart == null) continue;

            currentPart.localPosition = toExplodedState ? enginePartsList[i].explodedLocalPosition : defaultLocalPositions[i];
            currentPart.localRotation = toExplodedState ? Quaternion.Euler(enginePartsList[i].explodedLocalRotation) : defaultLocalRotations[i];
        }
    }
}