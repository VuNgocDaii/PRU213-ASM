using UnityEditor;

public class PolishCowInteractionSetup
{
    // Giữ menu cũ để không đổi thói quen sử dụng.
    [MenuItem("Tools/PolishCow/Setup Interaction Test")]
    [MenuItem("Tools/PolishCow/Setup Main Interaction")]
    public static void SetupInteractionTest()
    {
        PolishCowWorkspaceAudit.SetupMainInteraction();
    }
}
