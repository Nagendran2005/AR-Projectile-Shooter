using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARPartToggleManager : MonoBehaviour
{
    [Header("Director Reference Integration")]
    [Tooltip("Drag the GameObject with your new ARDirectPlacementDirector script here.")]
    [SerializeField] private ARDirectPlacementDirector placementDirector;

    [Header("Direct Scene Target Configuration")]
    [Tooltip("The existing live scene GameObject that is currently controlled by the Director.")]
    [SerializeField] private GameObject existingGameObject;

    [Header("Alternative Swap Assets")]
    [Tooltip("The alternative detailed scene GameObject that should be unhidden.")]
    [SerializeField] private GameObject detailedAlternativeObject;

    [Tooltip("The specific Transform slot that dictates exactly where the detailed object should position and orient itself.")]
    [SerializeField] private Transform detailedTargetTransformSlot;

    [Header("Animate Scaling Curves")]
    [SerializeField] private float transitionDuration = 0.25f;

    private bool isShowingDetailedParts = false;
    private Vector3 originalObjectTargetScale = Vector3.one;
    private Vector3 detailedObjectTargetScale = Vector3.one;

    private void Start()
    {
        // Cache the default design scales from the scene before any changes occur
        if (existingGameObject != null)
        {
            originalObjectTargetScale = existingGameObject.transform.localScale;
        }

        if (detailedAlternativeObject != null)
        {
            // Use the scale from the target transform slot if assigned, otherwise fallback to its own scale
            detailedObjectTargetScale = (detailedTargetTransformSlot != null) ? detailedTargetTransformSlot.localScale : detailedAlternativeObject.transform.localScale;

            // Ensure the alternative breakdown object starts deactivated upon application launch
            detailedAlternativeObject.SetActive(false);
        }
    }

    /// <summary>
    /// Call this function from your 'PARTS' Button OnClick() event handler loop.
    /// </summary>
    public void ExecutePartSwapSwapSequence()
    {
        if (isShowingDetailedParts) return;
        if (existingGameObject == null || detailedAlternativeObject == null)
        {
            Debug.LogWarning("[ARPartToggleManager] Please assign both the Existing and Detailed objects in the inspector!");
            return;
        }

        isShowingDetailedParts = true;

        // 1. Hide the primary existing gameobject cleanly
        existingGameObject.SetActive(false);

        // 2. If a custom transform slot is provided, match its position and rotation perfectly before revealing
        if (detailedTargetTransformSlot != null)
        {
            detailedAlternativeObject.transform.position = detailedTargetTransformSlot.position;
            detailedAlternativeObject.transform.rotation = detailedTargetTransformSlot.rotation;
        }

        // 3. Unhide the alternative structure and scale it up smoothly to its target scale
        detailedAlternativeObject.SetActive(true);
        StartCoroutine(ExecuteScalingTransition(detailedAlternativeObject.transform, Vector3.zero, detailedObjectTargetScale));
    }

    /// <summary>
    /// Call this function from your 'BACK' Button OnClick() event handler loop.
    /// </summary>
    public void ExecuteReverseReversionSequence()
    {
        if (!isShowingDetailedParts) return;
        if (existingGameObject == null || detailedAlternativeObject == null) return;

        isShowingDetailedParts = false;

        // 1. Hide the alternative target asset group framework cleanly
        detailedAlternativeObject.SetActive(false);

        // 2. Bring back visibility to the primary baseline scene element structure seamlessly
        existingGameObject.SetActive(true);
        StartCoroutine(ExecuteScalingTransition(existingGameObject.transform, Vector3.zero, originalObjectTargetScale));
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
}