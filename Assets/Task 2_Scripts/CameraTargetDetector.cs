using UnityEngine;

public class CameraTargetDetector : MonoBehaviour
{
    public static CameraTargetDetector Instance;

    [Header("References")]
    [SerializeField] private Camera arCamera;

    [Header("Detection")]
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private float detectionDistance = 20f;

    [Header("Debug")]
    [SerializeField] private bool showDebugRay = true;

    private TargetController currentTarget;

    public TargetController CurrentTarget => currentTarget;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (arCamera == null)
            arCamera = Camera.main;
    }

    private void Update()
    {
        DetectTarget();
    }

    private void DetectTarget()
    {
        Ray ray = new Ray(arCamera.transform.position, arCamera.transform.forward);

        if (showDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * detectionDistance, Color.green);

        if (Physics.Raycast(ray, out RaycastHit hit, detectionDistance, targetLayer))
        {
            TargetController target = hit.collider.GetComponentInParent<TargetController>();

            if (target == null)
            {
                ClearHighlight();
                return;
            }

            // Same target, no need to update
            if (currentTarget == target)
                return;

            // Remove previous highlight
            if (currentTarget != null)
                currentTarget.SetHighlight(false);

            currentTarget = target;
            currentTarget.SetHighlight(true);
        }
        else
        {
            ClearHighlight();
        }
    }

    private void ClearHighlight()
    {
        if (currentTarget != null)
        {
            currentTarget.SetHighlight(false);
            currentTarget = null;
        }
    }

    // Called when a target is destroyed
    public void ClearCurrentTarget()
    {
        currentTarget = null;
    }
}