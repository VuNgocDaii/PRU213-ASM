using UnityEngine;

// Owned by base. Flying enemies depend on this contract, never on CowHealth.
public interface ICowDiseaseTarget
{
    Transform TargetTransform { get; }
    bool IsHealthy { get; }
    bool CanBeInfected { get; }
    void Infect();
}
