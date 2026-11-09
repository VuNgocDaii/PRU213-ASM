// Person 3 owns EnemyController, WaveSpawner and pooling under this folder.
public class EnemiesFeature : DefenseModule
{
    // Subscribe to Context.Game.OnWaveStarted in OnConnected.
    // EnemyController implements IEnemyTarget (and ISlowable), registers/unregisters on pool lifecycle.
    // Fly uses Context.Cows and ICowDiseaseTarget.Infect, without referencing the economy module.
}
