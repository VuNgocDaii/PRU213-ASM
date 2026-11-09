// Used by the workspace contract test to verify context propagation and lifecycle behavior.
public class DefenseProbeModule : DefenseModule
{
    public int InitializationCount { get; private set; }
    protected override void OnInitialized() { InitializationCount++; }
}
