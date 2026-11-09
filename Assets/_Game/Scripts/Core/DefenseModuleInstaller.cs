using UnityEngine;

[DefaultExecutionOrder(-100)]
public class DefenseModuleInstaller : MonoBehaviour
{
    [SerializeField] private DefenseSceneContext context;
    [SerializeField] private Transform[] featureRoots;
    private void Start()
    {
        if (context == null || !context.IsReady) { Debug.LogError("DefenseModuleInstaller: incomplete scene context.",this); return; }
        if (featureRoots == null) return;
        foreach (Transform root in featureRoots)
        {
            if (root == null) continue;
            foreach (DefenseModule module in root.GetComponentsInChildren<DefenseModule>(true)) module.Initialize(context);
        }
    }
}
