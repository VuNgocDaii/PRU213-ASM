// Person 1 owns this module plus Cows, Production and Orders. Add economy UI under this folder.
public class EconomyFeature : DefenseModule
{
    // Subscribe to Context.Game/Progress in OnConnected; unsubscribe in OnDisconnected.
    // CowHealth implements ICowDiseaseTarget and registers with Context.RegisterCow.
}
