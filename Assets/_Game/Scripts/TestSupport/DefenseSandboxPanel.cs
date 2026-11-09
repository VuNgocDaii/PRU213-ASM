using UnityEngine;

// Developer controls only for the dedicated team workspaces, never Main.
public class DefenseSandboxPanel : DefenseModule
{
    private void OnGUI()
    {
        if (Context == null) return;
        GUILayout.BeginArea(new Rect(12,Screen.height-225,330,213),GUI.skin.box);
        GUILayout.Label("TEAM WORKSPACE - TEST CONTROLS");
        GUILayout.Label("Credits: " + Context.Inventory.GetAmount(ItemType.Credits) + " | EXP: " + Context.Progress.Experience);
        GUILayout.Label("Targets: " + Context.Enemies.Count + " enemies / " + Context.Cows.Count + " cows");
        if (GUILayout.Button("Add 500 Credits")) Context.Inventory.Add(ItemType.Credits,500);
        if (GUILayout.Button("Add 495 EXP")) Context.Progress.AddExperience(495);
        if (GUILayout.Button("Damage castle (25)")) Context.CastleHealth.TakeDamage(25);
        if (GUILayout.Button("Begin next wave")) Context.Game.BeginNextWave();
        if (GUILayout.Button("Complete current wave")) Context.Game.CompleteWave(Context.Game.CurrentWave);
        if (GUILayout.Button("Pause / Resume")) Context.Game.SetPaused(Context.Game.State != GameState.Paused);
        GUILayout.EndArea();
    }
}
