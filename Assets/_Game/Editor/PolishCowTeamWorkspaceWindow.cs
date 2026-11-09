using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PolishCowTeamWorkspaceWindow : EditorWindow
{
    private static readonly string[] Roles = { "Economy", "Building", "Enemies", "Towers" };
    private static readonly string[] Titles = { "1. Kinh tế và bệnh bò", "2. Xây dựng và nâng cấp", "3. Quái và wave", "4. Tháp và UI chiến đấu" };
    private static readonly string[] Tasks = {
        "Bò bệnh, thuốc, sữa hỏng, máy chế biến, bán sữa và delivery.",
        "Catalog xây/nâng/bán, kiểm tra vị trí, giá và hình công trình.",
        "Spawn, waypoint, ruồi, pooling, thưởng wave và boss wave 10.",
        "Nhắm/bắn, đạn, slow/AOE, shop khóa cấp và canvas chiến đấu."
    };
    [MenuItem("Tools/PolishCow/Team Base/Open Team Workspaces")]
    public static void Open() { var window = GetWindow<PolishCowTeamWorkspaceWindow>("PolishCow Team"); window.minSize = new Vector2(540,470); }
    private void OnGUI()
    {
        EditorGUILayout.LabelField("FOUR INDEPENDENT WORKSPACES",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Mỗi người sửa scene và prefab module của mình. Core, Main, kho, Player và API chung do người tích hợp giữ.",MessageType.Info);
        for (int i = 0; i < Roles.Length; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(Titles[i],EditorStyles.boldLabel);
            EditorGUILayout.LabelField(Tasks[i],EditorStyles.wordWrappedLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Mở scene thử")) OpenScene("Assets/_Game/Scenes/Workspaces/"+Roles[i]+"Workspace.unity");
            if (GUILayout.Button("Chọn prefab module"))
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Modules/"+Roles[i]+"/"+Roles[i]+"Feature.prefab");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space();
        if (GUILayout.Button("Mở Main — người tích hợp")) OpenScene("Assets/_Game/Scenes/Main.unity");
        EditorGUILayout.LabelField("Hướng dẫn: Docs/TEAM_4_WORKSTREAMS.md",EditorStyles.miniLabel);
    }
    private static void OpenScene(string path)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Exit Play Mode before switching team workspaces."); return; }
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
    }
}
