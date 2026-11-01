using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class PolishCowSceneBuilder
{
    [MenuItem("Tools/PolishCow/Create Main Scene")]
    public static void CreateMainScene()
    {
        // Chỉ tạo scene khi đang ở chế độ chỉnh sửa.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("PolishCow", "Hãy thoát Play Mode trước khi tạo scene.", "Đóng");
            return;
        }

        // Cho phép lưu các scene đang sửa hoặc hủy thao tác.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        string scenePath = "Assets/_Game/Scenes/Main.unity";
        if (File.Exists(scenePath))
        {
            bool overwriteConfirmed = EditorUtility.DisplayDialog(
                "PolishCow",
                "Main.unity đã tồn tại. Bạn có muốn ghi đè scene này không?",
                "Ghi đè",
                "Hủy");

            if (!overwriteConfirmed)
            {
                return;
            }
        }

        // Scene trống giúp tránh tạo trùng Camera và EventSystem.
        Scene mainScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();

        GameObject lightObject = new GameObject("Global Light 2D");
        Light2D globalLight = lightObject.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.color = Color.white;
        globalLight.intensity = 1f;

        // GameObject mới có vị trí, góc xoay bằng 0 và tỉ lệ bằng 1.
        GameObject gameSystems = new GameObject("GameSystems");
        CreateWorld();
        CreateCanvas();
        CreateEventSystem();

        // Tạo thư mục nếu cần rồi lưu bằng Unity Editor API.
        Directory.CreateDirectory("Assets/_Game/Scenes");
        AssetDatabase.Refresh();
        bool sceneSaved = EditorSceneManager.SaveScene(mainScene, scenePath);
        if (sceneSaved)
        {
            Debug.Log("Đã tạo và lưu scene PolishCow tại " + scenePath);
        }
        else
        {
            EditorUtility.DisplayDialog("PolishCow", "Không lưu được Main.unity. Hãy kiểm tra Console và quyền ghi thư mục.", "Đóng");
        }
    }

    private static void CreateCamera()
    {
        // Camera 2D có đúng một AudioListener.
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera mainCamera = cameraObject.AddComponent<Camera>();
        mainCamera.orthographic = true;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>();
    }

    private static void CreateWorld()
    {
        // Các nhóm chỉ dùng để sắp xếp đối tượng trong Hierarchy.
        GameObject worldObject = new GameObject("World");
        string[] groupNames = { "Ground", "Buildings", "Animals" };

        foreach (string groupName in groupNames)
        {
            GameObject groupObject = new GameObject(groupName);
            groupObject.transform.SetParent(worldObject.transform, false);
            groupObject.transform.localPosition = Vector3.zero;
            groupObject.transform.localRotation = Quaternion.identity;
            groupObject.transform.localScale = Vector3.one;
        }
    }

    private static void CreateCanvas()
    {
        // Canvas Overlay tự co giãn theo độ phân giải màn hình.
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        // Các lớp UI phủ toàn Canvas và chưa có Image.
        string[] layerNames = { "HUD", "ScreenLayer", "PopupLayer", "NotificationLayer" };
        foreach (string layerName in layerNames)
        {
            GameObject layerObject = new GameObject(layerName, typeof(RectTransform));
            RectTransform layerTransform = layerObject.GetComponent<RectTransform>();
            layerTransform.SetParent(canvasObject.transform, false);
            layerTransform.localPosition = Vector3.zero;
            layerTransform.localRotation = Quaternion.identity;
            layerTransform.localScale = Vector3.one;
            layerTransform.anchorMin = Vector2.zero;
            layerTransform.anchorMax = Vector2.one;
            layerTransform.offsetMin = Vector2.zero;
            layerTransform.offsetMax = Vector2.zero;
        }
    }

    private static void CreateEventSystem()
    {
        // Scene mới chỉ có một EventSystem và một input module.
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        // Khi bật Input System mới hoặc Both, ưu tiên module mới.
        InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }
}
