using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PolishCowHUDSetup
{
    private const string SetupKey = "PolishCow.InventoryHUD.SetupCompleted";

    [MenuItem("Tools/PolishCow/Setup Inventory HUD")]
    public static void SetupInventoryHUD()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/_Game/Scenes/Main.unity")
        {
            Debug.LogError("Hãy mở scene Main và thoát Play Mode trước khi gắn HUD.");
            return;
        }

        GameObject systems = GameObject.Find("GameSystems");
        GameObject hudObject = GameObject.Find("Canvas/HUD");
        if (systems == null || hudObject == null)
        {
            Debug.LogError("Main cần có GameSystems và Canvas/HUD. Không tạo lại scene.");
            return;
        }

        InventoryManager inventory = systems.GetComponent<InventoryManager>();
        if (inventory == null)
        {
            Debug.LogError("GameSystems chưa có InventoryManager.");
            return;
        }

        // Nhập tài nguyên TMP có sẵn trong package, không cài package mới.
        if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
        {
            UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TextMeshProUGUI).Assembly);
            string essentialsPath = Path.Combine(package.resolvedPath, "Package Resources/TMP Essential Resources.unitypackage");
            AssetDatabase.ImportPackage(essentialsPath, false);
        }

        // Font Arial hỗ trợ các ký tự tiếng Việt của HUD.
        string fontFolder = "Assets/_Game/Art/UI";
        string fontAssetPath = fontFolder + "/InventoryHUD Font.asset";
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
        if (fontAsset == null)
        {
            string systemFontPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Fonts), "arial.ttf");
            if (!File.Exists(systemFontPath))
            {
                Debug.LogError("Không tìm thấy Arial để tạo font tiếng Việt cho HUD.");
                return;
            }

            Directory.CreateDirectory(fontFolder);
            string sourceFontPath = fontFolder + "/InventoryHUD Arial.ttf";
            if (!File.Exists(sourceFontPath))
            {
                File.Copy(systemFontPath, sourceFontPath);
            }
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(sourceFontPath, ImportAssetOptions.ForceSynchronousImport);
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourceFontPath);
            if (sourceFont == null)
            {
                Debug.LogError("Unity chưa nhập được font Arial. Hãy chạy lại Setup Inventory HUD sau khi import hoàn tất.");
                return;
            }
            fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
            fontAsset.name = "InventoryHUD Font";
            AssetDatabase.CreateAsset(fontAsset, fontAssetPath);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            fontAsset.TryAddCharacters("Tiền: Thức ăn: Sữa thô: Sữa đóng chai: 0123456789");
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
        }

        InventoryHUD hud = hudObject.GetComponent<InventoryHUD>();
        if (hud == null)
        {
            hud = Undo.AddComponent<InventoryHUD>(hudObject);
        }

        string[] objectNames = { "CreditsText", "FeedText", "RawMilkText", "BottledMilkText" };
        string[] iconNames = { "CreditsIcon", "FeedIcon", "RawMilkIcon", "BottledMilkIcon" };
        string[] fieldNames = { "creditsText", "feedText", "rawMilkText", "bottledMilkText" };
        ItemType[] itemTypes = { ItemType.Credits, ItemType.Feed, ItemType.RawMilk, ItemType.BottledMilk };

        SerializedObject serializedHUD = new SerializedObject(hud);
        serializedHUD.FindProperty("inventory").objectReferenceValue = inventory;
        for (int index = 0; index < objectNames.Length; index++)
        {
            // Tạo chỗ đặt icon; sprite được kéo thả trong Inspector.
            Transform existingIcon = hudObject.transform.Find(iconNames[index]);
            if (existingIcon == null)
            {
                GameObject iconObject = new GameObject(iconNames[index], typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(iconObject, "Create Inventory HUD Icon");
                iconObject.transform.SetParent(hudObject.transform, false);
                iconObject.layer = hudObject.layer;
                Image iconImage = Undo.AddComponent<Image>(iconObject);
                iconImage.raycastTarget = false;
                iconImage.preserveAspect = true;

                RectTransform iconRect = iconImage.rectTransform;
                iconRect.anchorMin = new Vector2(0f, 1f);
                iconRect.anchorMax = new Vector2(0f, 1f);
                iconRect.pivot = new Vector2(0f, 1f);
                iconRect.sizeDelta = new Vector2(40f, 40f);
                iconRect.anchoredPosition3D = new Vector3(24f, -28f - index * 52f, 0f);
            }

            // Dùng lại dòng chữ đã có để tránh tạo trùng khi chạy lại menu.
            Transform existingText = hudObject.transform.Find(objectNames[index]);
            GameObject textObject;
            if (existingText == null)
            {
                textObject = new GameObject(objectNames[index], typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(textObject, "Create Inventory HUD Text");
                textObject.transform.SetParent(hudObject.transform, false);
            }
            else
            {
                textObject = existingText.gameObject;
            }

            textObject.layer = hudObject.layer;
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = Undo.AddComponent<TextMeshProUGUI>(textObject);
            }
            Undo.RecordObject(text, "Configure Inventory HUD Text");
            text.font = fontAsset;
            text.fontSize = 32f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            text.text = inventory.GetAmount(itemTypes[index]).ToString();

            RectTransform rect = text.rectTransform;
            Undo.RecordObject(rect, "Position Inventory HUD Text");
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(520f, 48f);
            rect.anchoredPosition3D = new Vector3(80f, -24f - index * 52f, 0f);
            serializedHUD.FindProperty(fieldNames[index]).objectReferenceValue = text;
        }

        serializedHUD.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        if (EditorSceneManager.SaveScene(scene))
        {
            SessionState.SetBool(SetupKey, true);
            File.WriteAllText("Temp/PolishCowHUDSetup.done", "InventoryHUD references assigned and Main saved.");
            Selection.activeGameObject = hudObject;
            Debug.Log("Đã gắn InventoryHUD, bốn dòng TMP và InventoryManager, rồi lưu Main.");
        }
    }
}
