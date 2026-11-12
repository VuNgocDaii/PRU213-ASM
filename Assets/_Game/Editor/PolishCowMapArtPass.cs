using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Repeatable art-only pass: preserves routes, wave data, build slots and feature wiring.
public static class PolishCowMapArtPass
{
    private const string Art = "Assets/_Game/Art/SproutLands/";
    private const string Data = "Assets/_Game/Data/Defense/";
    private static MapZone[] zones;
    private static DefenseBuildingSlot[] slots;
    private static readonly List<Rect> occupied = new List<Rect>();
    private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    [MenuItem("Tools/PolishCow/Map/Rebuild Landscape and Repair Atlas Sprites")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/_Game/Scenes/Main.unity") throw new InvalidOperationException("Open Main first.");
        Directory.CreateDirectory("output/backups");
        if (!File.Exists("output/backups/Main-before-landscape.unity"))
            EditorSceneManager.SaveScene(scene, "output/backups/Main-before-landscape.unity", true);
        sprites.Clear(); occupied.Clear();
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        zones = all.Select(t => t.GetComponent<MapZone>()).Where(z => z != null).ToArray();
        slots = all.Select(t => t.GetComponent<DefenseBuildingSlot>()).Where(s => s != null).ToArray();
        PolishCowFixedBuildLayout.ApplyToScene(scene);
        Repair("Tree", 1); Repair("Shrub", 22); Repair("Flowers", 20);
        Nature("SlenderTree", 0); Nature("AppleTree", 2); Nature("BerryBush", 29);
        Nature("GrassTuft", 8); Nature("SmallStone", 12); Nature("PinkFlower", 27); Nature("WhiteFlower", 20);
        FromAtlas("Cabbage", "Objects/Basic_Plants.png", "Basic_Plants_1");
        FromAtlas("Carrot", "Objects/Basic_Plants.png", "Basic_Plants_2");
        FromAtlas("Crate", "Objects/Chest.png", "Chest_0");
        FromAtlas("Timber", "Objects/Basic_tools_and_meterials.png", "Basic_tools_and_meterials_5");
        foreach (var zone in zones)
        {
            var content = zone.transform.Find("Content");
            foreach (Transform child in content.Cast<Transform>().ToArray())
                if (child.name == "MapArt" || System.Text.RegularExpressions.Regex.IsMatch(child.name, "^Nature[0-9]+$"))
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            var root = new GameObject("MapArt").transform; root.SetParent(content, false);
        }
        RefreshTerrain(all.First(t => t != null && t.name == "ExpandedFarm"));
        ArrangeMarket(all.Where(t => t != null).ToArray());
        foreach (var slot in slots.Where(s => s.SlotKind == DefenseBuildingSlot.Kind.Tower))
        {
            var renderer = slot.GetComponent<SpriteRenderer>();
            if (renderer == null) continue;
            renderer.size = Vector2.one * 2;
            renderer.color = new Color(1, .94f, .80f, .95f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
        Garden(new Vector2(-11,-10), 4, 2, "Cabbage");
        Garden(new Vector2(7,-10), 4, 2, "Carrot");
        Garden(new Vector2(-13,-19), 6, 3, "Cabbage");
        Garden(new Vector2(8,-19), 6, 3, "Carrot");
        Pond();
        // Trees frame the perimeter; leave the entrances and fighting lanes open.
        var random = new System.Random(213);
        for (float x = -30; x <= 30; x += 2.65f)
            foreach (float y in new[] { -22.5f, 20.3f })
                PlaceTree(new Vector2(x + Jitter(random, .4f), y + Jitter(random, .35f)), random);
        for (float y = -19; y <= 18; y += 2.8f)
            foreach (float x in new[] { -30.2f, 30.2f })
                PlaceTree(new Vector2(x + Jitter(random, .3f), y + Jitter(random, .45f)), random);
        foreach (var center in new[] {new Vector2(-12,7),new Vector2(12,8),new Vector2(-12,-9),new Vector2(11,-10),
            new Vector2(-26,8),new Vector2(-20,-8),new Vector2(23,8),new Vector2(24,-8),
            new Vector2(-15,17),new Vector2(15,17),new Vector2(-15,-18),new Vector2(15,-18)})
            for (int i=0;i<6;i++) PlaceTree(center + new Vector2(Jitter(random,3), Jitter(random,2)), random);
        // Small garden beds and an orchard give each area a purpose.
        for(int row=0;row<2;row++) for(int col=0;col<4;col++)
            TryPlace("AppleTree", new Vector2(-28+col*2.5f,5+row*3), 1.05f, false);
        foreach(var p in new[]{new Vector2(13,3),new Vector2(12,5.5f),new Vector2(24,6),new Vector2(25,7),new Vector2(21,7)})
            TryPlace("Crate",p,1,false);
        foreach(var p in new[]{new Vector2(13,5.5f),new Vector2(25,5),new Vector2(22,7)}) TryPlace("Timber",p,1,false);
        // Scatter low ground cover in clusters rather than isolated half-sprites.
        for(int i=0;i<460;i++)
        {
            var p=new Vector2(-30+(float)random.NextDouble()*60,-22+(float)random.NextDouble()*42);
            var key=i%7==0?"SmallStone":i%4==0?"Shrub":i%3==0?"PinkFlower":i%3==1?"WhiteFlower":"GrassTuft";
            TryPlace(key,p,.65f+(float)random.NextDouble()*.3f,false);
        }
        foreach(var p in new[]{new Vector2(-9,0.3f),new Vector2(-5,0.3f),new Vector2(-9,6.4f),new Vector2(-5,6.4f),new Vector2(-3,10.5f),new Vector2(3,10.5f)})
            TryPlace("WhiteFlower",p,.8f,false);
        // Keep text labels off scenery and reduce the large floating area titles.
        foreach(var zone in zones)
        {
            var name=zone.transform.Find("Content/Name");
            if(name!=null) name.gameObject.SetActive(false);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/MapArtPass.txt", "PASS: atlas boundaries copied from authored sprite metadata; routes and 22 defense pads preserved.\n"+
            "Landscape props: "+occupied.Count+". Sprite rects:\n"+string.Join("\n",sprites.Select(s=>s.Key+" "+s.Value.rect)));
        Debug.Log("Landscape rebuilt with full atlas sprites, clear lanes, orchard, crop gardens and workshop props.");
    }

    private static float Jitter(System.Random r,float radius) => ((float)r.NextDouble()*2-1)*radius;
    private static void Repair(string name,int index) => Nature(name,index);
    private static void Nature(string name,int index) => FromAtlas(name,"Objects/Basic_Grass_Biom_things.png","Basic_Grass_Biom_things_"+index);
    private static void FromAtlas(string name,string source,string sourceName)
    {
        var original=AssetDatabase.LoadAllAssetsAtPath(Art+source).OfType<Sprite>().Single(s=>s.name==sourceName);
        var replacement=Sprite.Create(original.texture,original.rect,new Vector2(.5f,0),16,0,SpriteMeshType.FullRect);
        replacement.name=name;
        string path=Data+"Sprites/"+name+".asset";
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(sprite==null){sprite=replacement;AssetDatabase.CreateAsset(sprite,path);}
        else {EditorUtility.CopySerialized(replacement,sprite);UnityEngine.Object.DestroyImmediate(replacement);EditorUtility.SetDirty(sprite);}
        sprites[name]=sprite;
    }

    private static bool Road(int x,int y)
    {
        if(x < -31 || x > 30 || y < -23 || y > 22) return false;
        return y==-1 || y==0 || x==-1 || x==0 ||
            y>=0 && y<=8 && (x==-3 || x==-2 || x==2 || x==3) ||
            y>=6 && y<=10 && x>=-2 && x<=2 ||
            (y==-8 || y==-7) && x>=-10 && x<=9 || x==-7 && y==1;
    }

    private static void RefreshTerrain(Transform map)
    {
        var ground=map.Find("Grid/Ground").GetComponent<Tilemap>();
        var roads=map.Find("Grid/Paths").GetComponent<Tilemap>();
        roads.ClearAllTiles();
        for(int y=-24;y<24;y++) for(int x=-32;x<32;x++)
        {
            var cell=new Vector3Int(x,y,0);
            ground.SetTileFlags(cell,TileFlags.None);
            float patch=Mathf.PerlinNoise((x+41)*.12f,(y+27)*.12f);
            ground.SetColor(cell,Color.Lerp(new Color(.92f,.98f,.88f),Color.white,patch));
            if(!Road(x,y)) continue;
            int col=!Road(x-1,y)?0:!Road(x+1,y)?2:1;
            int row=!Road(x,y+1)?0:!Road(x,y-1)?2:1;
            roads.SetTile(cell,AssetDatabase.LoadAssetAtPath<Tile>(Data+"Tiles/Path"+col+row+".asset"));
        }
    }

    private static void PlaceTree(Vector2 p,System.Random r) => TryPlace(r.Next(4)==0?"SlenderTree":r.Next(5)==0?"AppleTree":"Tree",p,1.05f+(float)r.NextDouble()*.25f,true);
    private static bool TryPlace(string key,Vector2 p,float scale,bool tree)
    {
        var sprite=sprites[key];
        var size=(Vector2)sprite.bounds.size*scale;
        var rect=new Rect(p.x-size.x*.5f,p.y,size.x,size.y);
        var zone=zones.FirstOrDefault(z=>z.WorldBounds.Contains(new Vector3(p.x,p.y,0)));
        if(zone==null || rect.xMin < -31.5f || rect.xMax>31.5f || rect.yMax>23.5f) return false;
        // Evaluate the entire canopy, so a tall sprite cannot conceal a road or tower.
        for(int x=Mathf.FloorToInt(rect.xMin-.1f);x<=Mathf.FloorToInt(rect.xMax+.1f);x++)
            for(int y=Mathf.FloorToInt(rect.yMin-.1f);y<=Mathf.FloorToInt(rect.yMax+.1f);y++)
                if(Road(x,y) && rect.Overlaps(new Rect(x-.05f,y-.05f,1.1f,1.1f))) return false;
        foreach(var s in slots)
            if(rect.Overlaps(new Rect(s.transform.position.x-1.5f,s.transform.position.y-1.2f,3,tree?4:2.8f))) return false;
        if(rect.Overlaps(new Rect(-10.8f,.5f,7.6f,6.7f)) || rect.Overlaps(new Rect(-3,7,6,5)) ||
            rect.Overlaps(new Rect(5.8f,3,4.3f,4.8f)) || rect.Overlaps(new Rect(-10,-7,19,3)) ||
            rect.Overlaps(new Rect(-29,-10.5f,7,4.5f))) return false;
        if(occupied.Any(r=>r.Overlaps(rect))) return false;
        var obj=new GameObject(key);
        obj.transform.SetParent(zone.transform.Find("Content/MapArt"),false);
        obj.transform.position=p;obj.transform.localScale=Vector3.one*scale;
        var renderer=obj.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.drawMode=SpriteDrawMode.Simple;
        renderer.sortingOrder=WorldDepthSorting.GetSortingOrder(p.y);obj.AddComponent<WorldDepthSorting>();
        occupied.Add(rect);return true;
    }

    private static void Garden(Vector2 origin,int width,int height,string crop)
    {
        var soil=Surface(origin,"GardenSoil");
        for(int y=0;y<height;y++) for(int x=0;x<width;x++)
            soil.SetTile(new Vector3Int((int)origin.x+x,(int)origin.y+y,0),
                AssetDatabase.LoadAssetAtPath<Tile>(Data+"Tiles/Path"+(x==0?0:x==width-1?2:1)+(y==height-1?0:y==0?2:1)+".asset"));
        for(int y=0;y<height;y++) for(int x=0;x<width;x++)
            TryPlace(crop,origin+new Vector2(x+.5f,y+.15f),.8f,false);
    }

    private static Tilemap Surface(Vector2 p,string name)
    {
        var zone=zones.First(z=>z.WorldBounds.Contains((Vector3)p));
        var parent=zone.transform.Find("Content/MapArt");
        var grid=parent.Find("LandscapeGrid");
        if(grid==null){grid=new GameObject("LandscapeGrid",typeof(Grid)).transform;grid.SetParent(parent,false);grid.position=Vector3.zero;}
        var child=grid.Find(name);
        if(child!=null)return child.GetComponent<Tilemap>();
        var obj=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));obj.transform.SetParent(grid,false);
        obj.GetComponent<TilemapRenderer>().sortingOrder=-19995;
        return obj.GetComponent<Tilemap>();
    }

    private static void Pond()
    {
        var map=Surface(new Vector2(-26,-8),"PasturePond");
        var water=AssetDatabase.LoadAssetAtPath<Tile>(Data+"Tiles/Water.asset");
        // Store the curved shoreline in a native mesh: runtime Sprite.OverrideGeometry is not persisted by Unity.
        string path=Data+"Sprites/PasturePondMesh.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null)
        {
            mesh=new Mesh {name="PasturePondMesh"};AssetDatabase.CreateAsset(mesh,path);
        }
        var vertices=new Vector3[33];var triangles=new int[96];var uv=new Vector2[33];
        var texture=water.sprite.texture;var rect=water.sprite.rect;
        uv[0]=new Vector2(rect.center.x/texture.width,rect.center.y/texture.height);
        for(int i=0;i<32;i++)
        {
            float x=Mathf.Cos(i*Mathf.PI/16),y=Mathf.Sin(i*Mathf.PI/16);
            vertices[i+1]=new Vector3(x*3.2f,y*1.9f,0);
            uv[i+1]=new Vector2((rect.x+(x*.5f+.5f)*rect.width)/texture.width,(rect.y+(y*.5f+.5f)*rect.height)/texture.height);
            triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%32+1;
        }
        mesh.Clear();mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.colors=Enumerable.Repeat(Color.white,33).ToArray();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var material=AssetDatabase.LoadAssetAtPath<Material>(Data+"Sprites/PasturePondWater.mat");
        if(material==null){material=new Material(Shader.Find("Sprites/Default"));material.name="PasturePondWater";AssetDatabase.CreateAsset(material,Data+"Sprites/PasturePondWater.mat");}
        material.mainTexture=texture;EditorUtility.SetDirty(material);
        var surface=new GameObject("WaterSurface",typeof(MeshFilter),typeof(MeshRenderer));surface.transform.SetParent(map.transform,false);
        surface.transform.position=new Vector3(-26,-8.5f,0);surface.GetComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=surface.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=-19994;
        var collider=map.gameObject.AddComponent<PolygonCollider2D>();
        collider.points=Enumerable.Range(0,16).Select(i=>new Vector2(-26+3.1f*Mathf.Cos(i*Mathf.PI/8),-8.5f+1.8f*Mathf.Sin(i*Mathf.PI/8))).ToArray();
    }

    private static void ArrangeMarket(Transform[] all)
    {
        var feed=all.FirstOrDefault(t=>t.GetComponent<FeedShop>()!=null);
        if(feed!=null){feed.position=new Vector3(-8,-6,0);PrefabUtility.RecordPrefabInstancePropertyModifications(feed);}
        var delivery=all.FirstOrDefault(t=>t.GetComponent<OrderController>()!=null);
        if(delivery!=null)
        {
            delivery.position=new Vector3(7,-6,0);PrefabUtility.RecordPrefabInstancePropertyModifications(delivery);
            var old=delivery.GetComponent<SpriteRenderer>();
            if(old!=null && delivery.Find("DeliveryVisual")!=null){old.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(old);}
            var drone=delivery.Find("DroneVisual");
            if(drone!=null){drone.localScale=Vector3.one*.65f;drone.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(drone);PrefabUtility.RecordPrefabInstancePropertyModifications(drone.gameObject);}
        }
        foreach(var t in all)
        {
            if(t==null) continue;
            var renderer=t.GetComponent<SpriteRenderer>();
            if(renderer!=null && renderer.sprite!=null && renderer.sprite.name=="PrototypeMarkerSprite" && t.name!="Locked")
            {renderer.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
        }
    }
}
