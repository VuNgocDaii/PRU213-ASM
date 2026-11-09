using System;
using UnityEngine;

// Put the feature's composition root under this base. Awake only gathers local references.
public abstract class DefenseModule : MonoBehaviour
{
    public DefenseSceneContext Context { get; private set; }
    public void Initialize(DefenseSceneContext context)
    {
        if (context == null || !context.IsReady) throw new ArgumentException("A fully wired DefenseSceneContext is required.");
        if (Context == context) return;
        if (Context != null && isActiveAndEnabled) OnDisconnected();
        Context = context;
        OnInitialized();
        if (isActiveAndEnabled) OnConnected();
    }
    protected virtual void OnInitialized() { }
    protected virtual void OnConnected() { }
    protected virtual void OnDisconnected() { }
    protected virtual void OnEnable() { if (Context != null) OnConnected(); }
    protected virtual void OnDisable() { if (Context != null) OnDisconnected(); }
}
