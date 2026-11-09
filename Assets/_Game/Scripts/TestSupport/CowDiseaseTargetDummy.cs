using UnityEngine;

// Test-only fixture for fly behavior while the economy module is being implemented.
public class CowDiseaseTargetDummy : DefenseModule, ICowDiseaseTarget
{
    public Transform TargetTransform => transform;
    public bool IsHealthy => Severity == 0;
    public bool CanBeInfected => isActiveAndEnabled && Severity < 3;
    public int Severity { get; private set; }
    protected override void OnConnected() { Context.RegisterCow(this); }
    protected override void OnDisconnected() { Context.UnregisterCow(this); }
    public void Infect() { if (CanBeInfected) Severity++; }
}
