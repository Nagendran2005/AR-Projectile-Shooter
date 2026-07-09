using UnityEngine;

public class EngineButtonBridge : MonoBehaviour
{
    private EngineExplosionManager activeEngineManager;

    private void Update()
    {
        // If we don't have the reference yet, look for the spawned engine in the scene
        if (activeEngineManager == null)
        {
            activeEngineManager = Object.FindFirstObjectByType<EngineExplosionManager>();
        }
    }

    /// <summary>
    /// Bind this to your EXPLORE UI Button
    /// </summary>
    public void CallExplore()
    {
        if (activeEngineManager != null)
        {
            activeEngineManager.TriggerExplodedView();
        }
        else
        {
            Debug.LogWarning("[EngineButtonBridge] No active EngineExplosionManager found in the scene to explore!");
        }
    }

    /// <summary>
    /// Bind this to your RESET / BACK UI Button
    /// </summary>
    public void CallReset()
    {
        if (activeEngineManager != null)
        {
            activeEngineManager.TriggerReassemblyView();
        }
        else
        {
            Debug.LogWarning("[EngineButtonBridge] No active EngineExplosionManager found in the scene to reset!");
        }
    }
}