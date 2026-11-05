using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PolishCowCowsSetup
{
    private const string CowsPrefabFolder = "Assets/_Game/Prefabs/Features/Cows/";
    private const string CowBarnPrefabPath = CowsPrefabFolder + "CowBarn.prefab";
    private const string CowTestScenePath = "Assets/_Game/Scenes/Tests/CowTest.unity";

    [MenuItem("Tools/PolishCow/Setup Cows Feature (Prefabs & Scene)")]
    public static void SetupCowsFeature()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[PolishCowCowsSetup] Hãy thoát Play Mode trước khi setup.");
            return;
        }

        CreateCowBarnPrefab();
        ConfigureCowTestScene();
        Debug.Log("[PolishCowCowsSetup] Setup hoàn tất tính năng Cows (Prefab CowBarn & CowTest scene).");
    }

    public static void CreateCowBarnPrefab()
    {
        if (!Directory.Exists(CowsPrefabFolder))
        {
            Directory.CreateDirectory(CowsPrefabFolder);
        }

        Scene previous = SceneManager.GetActiveScene();
        Scene scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scratch);

            // 1. Root: CowBarn
            GameObject root = new GameObject("CowBarn");
            FeatureReferences rootRefs = root.AddComponent<FeatureReferences>();
            CowUpgrade cowUpgrade = root.AddComponent<CowUpgrade>();

            // 2. Visual
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            SpriteRenderer barnRenderer = visual.AddComponent<SpriteRenderer>();
            visual.AddComponent<WorldDepthSorting>();

            // Lấy sprite từ prefab chuồng mẫu nếu có
            GameObject sourceBarn = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/World/Buildings/CowBarn.prefab");
            if (sourceBarn != null)
            {
                SpriteRenderer srcRenderer = sourceBarn.GetComponentInChildren<SpriteRenderer>(true);
                if (srcRenderer != null)
                {
                    barnRenderer.sprite = srcRenderer.sprite;
                    barnRenderer.color = srcRenderer.color;
                }
            }

            // 3. RoamingArea (Cố định, không di chuyển theo bò)
            GameObject roamingObj = new GameObject("RoamingArea");
            roamingObj.transform.SetParent(root.transform, false);
            roamingObj.transform.localPosition = new Vector3(0f, -1.5f, 0f);
            RoamingArea roamingArea = roamingObj.AddComponent<RoamingArea>();
            SerializedObject roamingData = new SerializedObject(roamingArea);
            roamingData.FindProperty("size").vector2Value = new Vector2(5f, 4f);
            roamingData.ApplyModifiedPropertiesWithoutUndo();

            // 4. Cow
            GameObject cow = new GameObject("Cow");
            cow.transform.SetParent(root.transform, false);
            cow.transform.localPosition = new Vector3(0f, -1.5f, 0f);

            SpriteRenderer cowRenderer = cow.AddComponent<SpriteRenderer>();
            GameObject sampleCow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Features/Cows/CowSample.prefab");
            if (sampleCow != null)
            {
                SpriteRenderer srcCow = sampleCow.GetComponent<SpriteRenderer>();
                if (srcCow != null)
                {
                    cowRenderer.sprite = srcCow.sprite;
                    cowRenderer.color = srcCow.color;
                    cowRenderer.sortingOrder = srcCow.sortingOrder;
                }
            }

            WorldDepthSorting cowSorting = cow.AddComponent<WorldDepthSorting>();
            SerializedObject sortingData = new SerializedObject(cowSorting);
            sortingData.FindProperty("feetOffsetY").floatValue = -0.2f;
            sortingData.ApplyModifiedPropertiesWithoutUndo();

            BoxCollider2D collider = cow.AddComponent<BoxCollider2D>();
            collider.offset = new Vector2(0f, -0.2f);
            collider.size = new Vector2(0.7f, 0.4f);

            Rigidbody2D rb = cow.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            CowMovement movement = cow.AddComponent<CowMovement>();
            SerializedObject moveData = new SerializedObject(movement);
            moveData.FindProperty("roamingArea").objectReferenceValue = roamingArea;
            moveData.FindProperty("cowRigidbody").objectReferenceValue = rb;
            moveData.FindProperty("cowSpriteRenderer").objectReferenceValue = cowRenderer;
            moveData.FindProperty("padding").vector2Value = new Vector2(0.35f, 0.4f);
            moveData.ApplyModifiedPropertiesWithoutUndo();

            CowController controller = cow.AddComponent<CowController>();
            SerializedObject ctrlData = new SerializedObject(controller);
            ctrlData.FindProperty("featureReferences").objectReferenceValue = rootRefs;
            ctrlData.FindProperty("cowUpgrade").objectReferenceValue = cowUpgrade;
            ctrlData.ApplyModifiedPropertiesWithoutUndo();

            CowInteraction interaction = cow.AddComponent<CowInteraction>();
            SerializedObject interactData = new SerializedObject(interaction);
            interactData.FindProperty("cowController").objectReferenceValue = controller;
            interactData.ApplyModifiedPropertiesWithoutUndo();

            GameObject interactionPoint = new GameObject("InteractionPoint");
            interactionPoint.transform.SetParent(cow.transform, false);

            PrefabUtility.SaveAsPrefabAsset(root, CowBarnPrefabPath);
            Debug.Log("[PolishCowCowsSetup] Đã tạo prefab CowBarn tại " + CowBarnPrefabPath);
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }
            EditorSceneManager.CloseScene(scratch, true);
        }
    }

    public static void ConfigureCowTestScene()
    {
        if (!File.Exists(CowTestScenePath))
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(CowTestScenePath, OpenSceneMode.Single);
        try
        {
            Transform work = null;
            foreach (Transform t in PolishCowWorkspaceAudit.Collect<Transform>(scene))
            {
                if (t.name == "FeatureWork")
                {
                    work = t;
                    break;
                }
            }

            if (work == null)
            {
                return;
            }

            RoamingArea roamingArea = work.GetComponentInChildren<RoamingArea>(true);
            CowSampleUpdate(work, roamingArea);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PolishCowCowsSetup] Đã cập nhật và lưu scene CowTest.");
        }
        finally
        {
            // Scene giữ mở để người dùng tiện kiểm tra
        }
    }

    private static void CowSampleUpdate(Transform work, RoamingArea roamingArea)
    {
        foreach (Transform child in work)
        {
            if (child.name == "CowSample")
            {
                // Tháo component tương tác mẫu cũ
                InventoryInteractionSample oldSample = child.GetComponent<InventoryInteractionSample>();
                if (oldSample != null)
                {
                    Object.DestroyImmediate(oldSample);
                }

                // Đảm bảo Rigidbody2D
                Rigidbody2D rb = child.GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    rb = child.gameObject.AddComponent<Rigidbody2D>();
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.gravityScale = 0f;
                    rb.freezeRotation = true;
                }

                // Gắn CowUpgrade ở gốc CowSample
                CowUpgrade upgrade = child.GetComponent<CowUpgrade>();
                if (upgrade == null)
                {
                    upgrade = child.gameObject.AddComponent<CowUpgrade>();
                }

                // Gắn CowMovement
                CowMovement movement = child.GetComponent<CowMovement>();
                if (movement == null)
                {
                    movement = child.gameObject.AddComponent<CowMovement>();
                }
                SerializedObject moveData = new SerializedObject(movement);
                if (roamingArea != null)
                {
                    moveData.FindProperty("roamingArea").objectReferenceValue = roamingArea;
                }
                moveData.FindProperty("cowRigidbody").objectReferenceValue = rb;
                moveData.FindProperty("cowSpriteRenderer").objectReferenceValue = child.GetComponent<SpriteRenderer>();
                moveData.FindProperty("padding").vector2Value = new Vector2(0.35f, 0.4f);
                moveData.ApplyModifiedPropertiesWithoutUndo();

                // Gắn CowController
                CowController controller = child.GetComponent<CowController>();
                if (controller == null)
                {
                    controller = child.gameObject.AddComponent<CowController>();
                }
                FeatureReferences refs = child.GetComponent<FeatureReferences>();
                SerializedObject ctrlData = new SerializedObject(controller);
                ctrlData.FindProperty("featureReferences").objectReferenceValue = refs;
                ctrlData.FindProperty("cowUpgrade").objectReferenceValue = upgrade;
                ctrlData.ApplyModifiedPropertiesWithoutUndo();

                // Gắn CowInteraction
                CowInteraction interaction = child.GetComponent<CowInteraction>();
                if (interaction == null)
                {
                    interaction = child.gameObject.AddComponent<CowInteraction>();
                }
                SerializedObject interactData = new SerializedObject(interaction);
                interactData.FindProperty("cowController").objectReferenceValue = controller;
                interactData.ApplyModifiedPropertiesWithoutUndo();

                break;
            }
        }
    }
}
