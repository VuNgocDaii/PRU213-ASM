using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PolishCowTowerLevelSetup
{
    private const string Folder = "Assets/_Game/Animation/Tower/Levels";
    private const string Art = "Assets/_Game/Art/Features/Towers/An_Tower/";

    [MenuItem("Tools/PolishCow/Towers/Configure Three Levels and Attack Animations")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Game/Animation/Tower", "Levels");
        ConfigureTower(0, "Arrow", "Tower_type1", "01", "Spritesheets", new[] { 7.5f, 9f, 10.5f });
        ConfigureTower(1, "Cannon", "Tower_type2", "04", "PNGs", new[] { 7f, 8.5f, 10f });
        ConfigureTower(2, "Ice", "Tower_type4", "08", "Spritesheets", new[] { 7f, 8f, 9f });
        ConfigureTower(3, "Sniper", "Tower_type4", "07", "Spritesheets", new[] { 13f, 15f, 17f });
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureTower(int index, string kind, string type, string number, string sheetFolder, float[] ranges)
    {
        var definition = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Game/Data/Defense/Tower" + index + ".asset");
        if (definition == null) throw new InvalidOperationException("Missing tower definition " + kind);
        definition.range = ranges[0];
        definition.rangeByLevel = ranges;
        EditorUtility.SetDirty(definition);
        string basePath = Art + type + "/Towers bases/PNGs/Tower " + number + ".png";
        var bases = Sprites(basePath);
        if (bases.Length == 1) bases = SliceBaseLevels(basePath, number);
        if (bases.Length != 3) throw new InvalidOperationException("Expected three base sprites for " + kind);
        var controllerPath = Folder + "/" + kind + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var layers = controller.layers;
        layers[0].defaultWeight = 1;
        controller.layers = layers;
        var machine = controller.layers[0].stateMachine;
        var idleSprites = new Sprite[3];
        for (int level = 1; level <= 3; level++)
        {
            var path = Art + type + "/Towers Weapons/Tower " + number + "/" + sheetFolder +
                "/Tower " + number + " - Level " + level.ToString("00") + " - Weapon.png";
            // Ignore isolated tiny sparks picked up by automatic slicing of the cannon sheet.
            var frames = Sprites(path).Where(s => s.rect.height >= s.texture.height * .1f).ToArray();
            if (frames.Length < 2) throw new InvalidOperationException("Missing weapon animation frames: " + path);
            idleSprites[level - 1] = frames[0];
            var idle = State(machine, "Idle" + level);
            idle.motion = Clip(kind + "Idle" + level, new[] { frames[0] }, true);
            var fire = State(machine, "Fire" + level);
            fire.motion = Clip(kind + "Fire" + level, frames, false);
            var transition = fire.transitions.FirstOrDefault();
            if (transition == null) transition = fire.AddTransition(idle);
            transition.destinationState = idle;
            transition.hasExitTime = true;
            transition.exitTime = 1;
            transition.hasFixedDuration = true;
            transition.duration = 0;
            if (level == 1) machine.defaultState = idle;
        }
        EditorUtility.SetDirty(controller);
        string prefabPath = "Assets/_Game/Prefabs/Features/Tower/Tower_" + kind + ".prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var tower = root.GetComponent<TowerController>();
            var body = root.GetComponent<SpriteRenderer>();
            var animator = root.GetComponentInChildren<Animator>(true);
            var weapon = animator.GetComponent<SpriteRenderer>();
            body.sprite = bases[0];
            weapon.sprite = idleSprites[0];
            weapon.transform.localScale = Vector3.one * (idleSprites[0].pixelsPerUnit / bases[0].pixelsPerUnit);
            weapon.transform.localRotation = Quaternion.identity;
            weapon.transform.localPosition = new Vector3(0, body.sprite.bounds.max.y - .35f, 0);
            animator.runtimeAnimatorController = controller;
            animator.enabled = true;
            animator.speed = 1;
            var data = new SerializedObject(tower);
            data.FindProperty("spriteRenderer").objectReferenceValue = body;
            data.FindProperty("weaponRenderer").objectReferenceValue = weapon;
            data.FindProperty("weaponAnimator").objectReferenceValue = animator;
            data.FindProperty("firePoint").objectReferenceValue = weapon.transform;
            data.FindProperty("projectilePool").objectReferenceValue = root.GetComponentInChildren<ProjectilePool>(true);
            SetSprites(data.FindProperty("levelSprites"), bases);
            SetSprites(data.FindProperty("levelWeaponSprites"), idleSprites);
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static Sprite[] Sprites(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
        .OrderBy(s => s.rect.x).ThenByDescending(s => s.rect.y).ToArray();

    private static Sprite[] SliceBaseLevels(string path, string number)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        bool readable = importer.isReadable;
        importer.isReadable = true;
        importer.SaveAndReimport();
        try
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var pixels = texture.GetPixels32();
            int width = texture.width / 3;
            var metadata = new SpriteMetaData[3];
            for (int level = 0; level < 3; level++)
            {
                int bottom = texture.height, top = -1;
                for (int y = 0; y < texture.height; y++)
                    for (int x = level * width; x < (level + 1) * width; x++)
                        if (pixels[y * texture.width + x].a > 8) { bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
                if (top < bottom) throw new InvalidOperationException("Empty base level " + level);
                metadata[level] = new SpriteMetaData { name = "Tower " + number + "_" + level,
                    rect = new Rect(level * width, bottom, width, top - bottom + 1), alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f,.5f) };
            }
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 50;
            importer.spritesheet = metadata;
        }
        finally
        {
            importer.isReadable = readable;
            importer.SaveAndReimport();
        }
        return Sprites(path);
    }

    private static AnimatorState State(AnimatorStateMachine machine, string name)
    {
        return machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name) ?? machine.AddState(name);
    }

    private static AnimationClip Clip(string name, Sprite[] frames, bool loop)
    {
        string path = Folder + "/" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip { name = name }; AssetDatabase.CreateAsset(clip, path); }
        clip.frameRate = 12;
        var keys = frames.Select((sprite, i) => new ObjectReferenceKeyframe { time = i / 12f, value = sprite }).ToList();
        keys.Add(new ObjectReferenceKeyframe { time = frames.Length / 12f, value = frames[frames.Length - 1] });
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void SetSprites(SerializedProperty array, Sprite[] sprites)
    {
        array.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }
}
