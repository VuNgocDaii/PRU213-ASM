using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PolishCowUIPolish
{
    private static readonly Color Panel = new Color(0.075f,0.13f,0.14f,0.95f);
    private static readonly Color Ink = new Color(0.97f,0.95f,0.87f);
    private static readonly Color Gold = new Color(0.87f,0.72f,0.44f);

    [MenuItem("Tools/PolishCow/Polish Main HUD")]
    public static void Apply()
    {
        Scene original = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || original.isDirty) throw new Exception("Open a saved scene in Edit Mode before polishing the HUD.");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Game/Art/UI/InventoryHUD Font.asset");
        if (font == null) font = TMP_Settings.defaultFontAsset;
        font.TryAddCharacters("NÔNG TRẠI SỮA TIỀN THỨC ĂN SỮA THÔ SỮA CHAI THÀNH CẤP ĐỢT THUỐC HỎNG GIAO HÀNG ĐƠN Chuẩn bị Đang chiến đấu Tạm dừng Chiến thắng Thất bại Đợt tiếp theo giây Thuốc Sữa hỏng Di chuyển Tương tác Nhấn · 0123456789/#()+");
        foreach (string path in new[]{"Assets/_Game/Scenes/Main.unity","Assets/_Game/Scenes/Workspaces/EconomyWorkspace.unity"})
        {
            if (!File.Exists(path)) continue;
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new Exception("Save " + path + " before polishing UI.");
                Polish(scene,font);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Cannot save " + path);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene,true); }
        }
        SceneManager.SetActiveScene(original); AssetDatabase.SaveAssets();
        Debug.Log("Polished HUD: inventory, castle health/EXP, delivery card and interaction hint.");
    }
    private static void Polish(Scene scene,TMP_FontAsset font)
    {
        InventoryHUD inventory = All<InventoryHUD>(scene).Single();
        Transform hud = inventory.transform;
        Image card = Card(hud,"InventoryCard"); Rect(card.rectTransform,new Vector2(0,1),new Vector2(24,-24),new Vector2(760,102));
        Text(card.transform,"Heading","NÔNG TRẠI SỮA",font,15,Gold,new Vector2(18,-10),new Vector2(720,22));
        var inventoryData = new SerializedObject(inventory);
        string[] fields = {"creditsText","feedText","rawMilkText","bottledMilkText"};
        string[] icons = {"CreditsIcon","FeedIcon","RawMilkIcon","BottledMilkIcon"};
        string[] labels = {"TIỀN","THỨC ĂN","SỮA THÔ","SỮA CHAI"};
        for (int i = 0; i < 4; i++)
        {
            float x = 18 + i * 184;
            var number = (TextMeshProUGUI)inventoryData.FindProperty(fields[i]).objectReferenceValue;
            number.transform.SetParent(card.transform,false); Style(number,font,28,Ink);
            Rect(number.rectTransform,new Vector2(0,1),new Vector2(x+38,-53),new Vector2(128,36));
            Text(card.transform,"ResourceLabel"+i,labels[i],font,15,new Color(0.73f,0.8f,0.76f),new Vector2(x+38,-34),new Vector2(138,20));
            Image icon = hud.GetComponentsInChildren<Image>(true).FirstOrDefault(image => image.name == icons[i]);
            if (icon != null) { icon.transform.SetParent(card.transform,false); icon.preserveAspect = true; icon.raycastTarget = false; Rect(icon.rectTransform,new Vector2(0,1),new Vector2(x,-52),new Vector2(30,30)); }
        }

        CastleHUD castle = All<CastleHUD>(scene).Single();
        Image castleCard = Component<Image>(castle.gameObject); Decorate(castleCard);
        Rect(castle.GetComponent<RectTransform>(),new Vector2(0,1),new Vector2(24,-138),new Vector2(560,170));
        var castleData = new SerializedObject(castle);
        var status = (TextMeshProUGUI)castleData.FindProperty("statusText").objectReferenceValue;
        Style(status,font,22,Ink); status.lineSpacing = 3;
        Rect(status.rectTransform,new Vector2(0,1),new Vector2(18,-14),new Vector2(524,124));
        Image hp = Bar(castle.transform,"Health",new Vector2(18,-147),new Color(0.42f,0.73f,0.51f),1);
        Image exp = Bar(castle.transform,"Experience",new Vector2(286,-147),new Color(0.43f,0.69f,0.83f),0);
        castleData.FindProperty("healthFill").objectReferenceValue = hp;
        castleData.FindProperty("experienceFill").objectReferenceValue = exp;
        castleData.ApplyModifiedPropertiesWithoutUndo();

        foreach (OrderUI orders in All<OrderUI>(scene))
        {
            var data = new SerializedObject(orders);
            Image orderCard = Component<Image>(orders.gameObject); Decorate(orderCard);
            Rect(orderCard.rectTransform,new Vector2(1,1),new Vector2(-24,-24),new Vector2(400,300));
            Text(orders.transform,"CardHeading","TRẠM GIAO SỮA",font,18,Gold,new Vector2(18,-16),new Vector2(364,26));
            var info = (TextMeshProUGUI)data.FindProperty("orderInfoText").objectReferenceValue;
            Style(info,font,24,Ink); info.lineSpacing = 5;
            Rect(info.rectTransform,new Vector2(0,1),new Vector2(18,-54),new Vector2(364,108));
            var progress = (TextMeshProUGUI)data.FindProperty("orderProgressText").objectReferenceValue;
            if (progress != null) { Style(progress,font,18,new Color(0.72f,0.8f,0.75f)); Rect(progress.rectTransform,new Vector2(0,1),new Vector2(18,-166),new Vector2(364,26)); }
            var notification = (TextMeshProUGUI)data.FindProperty("notificationText").objectReferenceValue;
            if (notification != null) { Style(notification,font,18,new Color(1,0.68f,0.58f)); Rect(notification.rectTransform,new Vector2(0,1),new Vector2(18,-195),new Vector2(364,34)); }
            var button = (Button)data.FindProperty("deliverButton").objectReferenceValue;
            Rect(button.GetComponent<RectTransform>(),new Vector2(0,1),new Vector2(18,-236),new Vector2(364,46));
            var buttonImage = button.GetComponent<Image>(); buttonImage.sprite = UISprite(); buttonImage.type = Image.Type.Sliced; buttonImage.color = Color.white;
            ColorBlock colors = button.colors; colors.normalColor = new Color(0.2f,0.49f,0.36f); colors.highlightedColor = new Color(0.29f,0.62f,0.45f); colors.selectedColor = colors.normalColor;
            colors.pressedColor = new Color(0.13f,0.36f,0.26f); colors.disabledColor = new Color(0.23f,0.3f,0.28f,0.75f); colors.fadeDuration = 0.12f; button.colors = colors;
            var label = button.GetComponentInChildren<TextMeshProUGUI>(true); Style(label,font,22,Ink); label.text = "GIAO HÀNG"; label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.offsetMin = Vector2.zero; label.rectTransform.offsetMax = Vector2.zero;
            foreach (Component component in orders.GetComponentsInChildren<Component>(true)) Record(component);
        }

        PlayerInteraction player = All<PlayerInteraction>(scene).Single();
        var hint = (TextMeshProUGUI)new SerializedObject(player).FindProperty("interactionHint").objectReferenceValue;
        if (hint != null)
        {
            Image hintCard = Card(hud,"InteractionHintCard"); hintCard.color = new Color(0.06f,0.1f,0.11f,0.9f);
            Rect(hintCard.rectTransform,new Vector2(0.5f,0),new Vector2(0,38),new Vector2(720,52));
            hint.transform.SetParent(hintCard.transform,false); Style(hint,font,22,Ink); hint.alignment = TextAlignmentOptions.Center;
            hint.rectTransform.anchorMin = Vector2.zero; hint.rectTransform.anchorMax = Vector2.one; hint.rectTransform.offsetMin = new Vector2(16,4); hint.rectTransform.offsetMax = new Vector2(-16,-4);
            var backdrop = Component<InteractionHintCard>(hintCard.gameObject); var data = new SerializedObject(backdrop); data.FindProperty("hint").objectReferenceValue = hint; data.FindProperty("background").objectReferenceValue = hintCard; data.ApplyModifiedPropertiesWithoutUndo();
        }
        var controls = Text(hud,"ControlLegend","WASD / Mũi tên: di chuyển     ·     E: tương tác",font,16,new Color(0.94f,0.96f,0.9f,0.85f),Vector2.zero,new Vector2(800,24));
        Rect(controls.rectTransform,new Vector2(0.5f,0),new Vector2(0,10),new Vector2(800,24)); controls.alignment = TextAlignmentOptions.Center;
        foreach (ProductionMachine machine in All<ProductionMachine>(scene))
        {
            Transform statusTransform = machine.transform.Find("StatusText");
            if (statusTransform != null)
            {
                var statusLabel = statusTransform.GetComponent<TextMeshPro>(); statusLabel.fontSize = 2.1f; statusLabel.font = font; statusLabel.rectTransform.sizeDelta = new Vector2(6,0.55f);
                statusTransform.localPosition = new Vector3(0,3,0); Record(statusLabel); Record(statusTransform);
            }
        }
    }
    private static Image Card(Transform parent,string name) { Image image = Component<Image>(Child(parent,name,true).gameObject); Decorate(image); return image; }
    private static void Decorate(Image image)
    {
        image.sprite = UISprite(); image.type = Image.Type.Sliced; image.color = Panel; image.raycastTarget = false;
        var outline = Component<Outline>(image.gameObject); outline.effectColor = new Color(Gold.r,Gold.g,Gold.b,0.4f); outline.effectDistance = new Vector2(1,-1);
    }
    private static Image Bar(Transform parent,string name,Vector2 position,Color color,float amount)
    {
        Image background = Component<Image>(Child(parent,name+"Track",true).gameObject); background.sprite = UISprite(); background.color = new Color(0.15f,0.23f,0.23f); background.raycastTarget = false;
        Rect(background.rectTransform,new Vector2(0,1),position,new Vector2(236,7));
        Image fill = Component<Image>(Child(background.transform,"Fill",true).gameObject); fill.sprite = UISprite(); fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.fillAmount = amount; fill.color = color; fill.raycastTarget = false;
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one; fill.rectTransform.offsetMin = Vector2.zero; fill.rectTransform.offsetMax = Vector2.zero; return fill;
    }
    private static TextMeshProUGUI Text(Transform parent,string name,string value,TMP_FontAsset font,float size,Color color,Vector2 position,Vector2 dimensions)
    {
        var text = Component<TextMeshProUGUI>(Child(parent,name,true).gameObject); Style(text,font,size,color); text.text = value;
        Rect(text.rectTransform,new Vector2(0,1),position,dimensions); return text;
    }
    private static void Style(TextMeshProUGUI text,TMP_FontAsset font,float size,Color color)
    { text.font = font; text.fontSize = size; text.enableAutoSizing = false; text.color = color; text.alignment = TextAlignmentOptions.TopLeft; text.raycastTarget = false; text.overflowMode = TextOverflowModes.Ellipsis; }
    private static void Rect(RectTransform rect,Vector2 anchor,Vector2 position,Vector2 size)
    { rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition3D = new Vector3(position.x,position.y,0); rect.sizeDelta = size; rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity; }
    private static Sprite UISprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    private static Transform Child(Transform parent,string name,bool ui)
    { Transform found = parent.Find(name); if (found != null) return found; GameObject obj = ui ? new GameObject(name,typeof(RectTransform)) : new GameObject(name); obj.transform.SetParent(parent,false); return obj.transform; }
    private static T Component<T>(GameObject obj) where T : Component { T component = obj.GetComponent<T>(); return component != null ? component : obj.AddComponent<T>(); }
    private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<T>(true)).ToArray();
    private static void Record(UnityEngine.Object target) { if (target != null && PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target); }
}
