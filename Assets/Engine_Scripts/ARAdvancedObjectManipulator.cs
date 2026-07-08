using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ARAdvancedObjectManipulator : MonoBehaviour
{
    [Header("Pivotal Center Configuration")]
    [Tooltip("Drag the central empty parent pivot container here.")]
    [SerializeField] private Transform centerPivot;

    [Header("Rotation Setup")]
    [SerializeField] private float rotationSpeed = 0.4f;
    [SerializeField] private float rotationSmoothness = 8f;
    [SerializeField] private LayerMask interactionLayer;

    [Header("Pinch Zoom Setup")]
    [SerializeField] private float zoomSpeed = 0.01f;
    [SerializeField] private float minScaleModifier = 0.3f;
    [SerializeField] private float maxScaleModifier = 3.0f;
    [SerializeField] private float zoomSmoothness = 8f;

    [Header("Auto Return Setup")]
    [SerializeField] private float idleDelayBeforeReset = 3f;
    [SerializeField] private float resetSpeed = 4f;

    // Target tracking values for smoothing interpolation
    private Quaternion targetRotation;
    private Vector3 targetScale;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Vector3 initialLocalScale;

    private bool isInteracting = false;
    private float lastInteractionTime = 0f;
    private bool isResetting = false;
    private Coroutine activeResetRoutine;

    private void Start()
    {
        if (centerPivot == null)
        {
            centerPivot = transform;
            Debug.LogWarning($"[ARAdvancedObjectManipulator] No 'Center Pivot' assigned on {gameObject.name}. Using root transform.", this);
        }

        // Cache precision initial states for the return timeline
        initialLocalPosition = centerPivot.localPosition;
        initialLocalRotation = centerPivot.localRotation;
        initialLocalScale = centerPivot.localScale;

        targetRotation = initialLocalRotation;
        targetScale = initialLocalScale;
        lastInteractionTime = Time.time;
    }

    private void Update()
    {
        HandleInputs();
        EvaluateIdleTimer();
    }

    private void LateUpdate()
    {
        // Smoothly move towards modern target values during active interaction
        if (!isResetting && centerPivot != null)
        {
            centerPivot.localRotation = Quaternion.Slerp(centerPivot.localRotation, targetRotation, Time.deltaTime * rotationSmoothness);
            centerPivot.localScale = Vector3.Lerp(centerPivot.localScale, targetScale, Time.deltaTime * zoomSmoothness);
        }
    }

    private void HandleInputs()
    {
        // Block interaction parsing if pointing directly at active UI canvas overlay spaces
        if (Input.touchCount > 0 && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
        {
            isInteracting = false;
            return;
        }

        // --- GESTURE MODULE 1: TWO-FINGER PINCH ZOOM ---
        if (Input.touchCount == 2)
        {
            isInteracting = true;
            isResetting = false;
            lastInteractionTime = Time.time;

            if (activeResetRoutine != null) StopCoroutine(activeResetRoutine);

            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // Find origin baseline tracking coordinates from the previous frame
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            // Calculate directional magnitude variations
            float deltaMagnitudeDiff = touchDeltaMag - prevTouchDeltaMag;

            // Apply scale shifts relative to initial baseline settings
            float scaleFactor = deltaMagnitudeDiff * zoomSpeed;
            targetScale += Vector3.one * scaleFactor;

            // Clamp values safely inside inspector boundaries
            float minS = initialLocalScale.x * minScaleModifier;
            float maxS = initialLocalScale.x * maxScaleModifier;
            targetScale.x = Mathf.Clamp(targetScale.x, minS, maxS);
            targetScale.y = Mathf.Clamp(targetScale.y, minS, maxS);
            targetScale.z = Mathf.Clamp(targetScale.z, minS, maxS);
            return;
        }

        // --- GESTURE MODULE 2: SINGLE-FINGER TRACKBALL MULTI-AXIS ROTATION ---
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                if (Physics.Raycast(ray, out _, Mathf.Infinity, interactionLayer))
                {
                    isInteracting = true;
                    isResetting = false;
                    if (activeResetRoutine != null) StopCoroutine(activeResetRoutine);
                }
                else
                {
                    isInteracting = false;
                }
            }

            if (touch.phase == TouchPhase.Moved && isInteracting)
            {
                lastInteractionTime = Time.time;

                // Calculate multi-axis orbital variables based on screen deltas
                float xRotation = touch.deltaPosition.x * rotationSpeed;
                float yRotation = touch.deltaPosition.y * rotationSpeed;

                // Trackball style rotation logic around both horizontal and vertical vectors
                Quaternion rotationX = Quaternion.AngleAxis(-xRotation, Camera.main.transform.up);
                Quaternion rotationY = Quaternion.AngleAxis(yRotation, Camera.main.transform.right);

                targetRotation = rotationX * rotationY * targetRotation;
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isInteracting = false;
                lastInteractionTime = Time.time;
            }
        }
    }

    private void EvaluateIdleTimer()
    {
        if (isInteracting || isResetting) return;

        // Triggers automated reset timeline if idle time passes the configuration threshold
        if (Time.time - lastInteractionTime >= idleDelayBeforeReset)
        {
            activeResetRoutine = StartCoroutine(ExecuteReturnToBaselineAnimation());
        }
    }

    private IEnumerator ExecuteReturnToBaselineAnimation()
    {
        isResetting = true;

        // Blends spatial values back to original configurations over time
        while (Quaternion.Angle(centerPivot.localRotation, initialLocalRotation) > 0.1f ||
               Vector3.Distance(centerPivot.localScale, initialLocalScale) > 0.01f ||
               Vector3.Distance(centerPivot.localPosition, initialLocalPosition) > 0.01f)
        {
            float step = Time.deltaTime * resetSpeed;

            centerPivot.localRotation = Quaternion.Slerp(centerPivot.localRotation, initialLocalRotation, step);
            centerPivot.localScale = Vector3.Lerp(centerPivot.localScale, initialLocalScale, step);
            centerPivot.localPosition = Vector3.Lerp(centerPivot.localPosition, initialLocalPosition, step);

            yield return null;
        }

        // Lock exactly back onto perfect default values
        centerPivot.localPosition = initialLocalPosition;
        centerPivot.localRotation = initialLocalRotation;
        centerPivot.localScale = initialLocalScale;

        // Synchronize internal working targets with baseline variables
        targetRotation = initialLocalRotation;
        targetScale = initialLocalScale;

        isResetting = false;
    }
}