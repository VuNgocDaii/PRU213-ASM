using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public class PolishCowSpriteSetup
{
    [MenuItem("Tools/PolishCow/Prepare Player Cow and Floor Sprites")]
    public static void PrepareSprites()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Hãy thoát Play Mode trước khi chuẩn bị sprite.");
            return;
        }

        // Chỉ xử lý đúng bốn ảnh đã kiểm tra.
        string playerPath = "Assets/_Game/Art/Actor/Character/ManGreen/SeparateAnim/Idle.png";
        string cowPath = "Assets/_Game/Art/Actor/Animal/Cow/SpriteSheetWhite.png";
        string cowSidePath = "Assets/_Game/Art/Actor/Animal/Cow/SpriteSheetWhiteSide.png";
        string floorPath = "Assets/_Game/Art/Backgrounds/Tilesets/TilesetFloor.png";

        // Kiểm tra trước để tránh cắt nhầm khi ảnh nguồn đã được thay thế.
        if (!CheckImage(playerPath, 64, 16))
        {
            return;
        }
        if (!CheckImage(cowPath, 32, 16))
        {
            return;
        }
        if (!CheckImage(cowSidePath, 48, 16))
        {
            return;
        }
        if (!CheckImage(floorPath, 352, 417))
        {
            return;
        }

        // Cùng 16 pixel = 1 unit, giữ đúng tỉ lệ giữa người, bò và đất.
        if (!PrepareSheet(playerPath, "PlayerIdle", 16, 16, 4, 1, 16f))
        {
            return;
        }
        if (!PrepareSheet(cowPath, "CowWhite", 16, 16, 2, 1, 16f))
        {
            return;
        }
        if (!PrepareSheet(cowSidePath, "CowWhiteSide", 16, 16, 3, 1, 16f))
        {
            return;
        }
        // 22 cột x 26 hàng; bỏ 1 hàng pixel trong suốt ở đáy ảnh.
        if (!PrepareSheet(floorPath, "Floor", 16, 16, 22, 26, 16f))
        {
            return;
        }

        Debug.Log("Đã chuẩn bị 4 ảnh: Player 4 sprite, bò 2 + 3 sprite, nền 572 tile. Không sửa scene hoặc tạo animation.");
    }

    private static bool CheckImage(string imagePath, int expectedWidth, int expectedHeight)
    {
        TextureImporter importer = AssetImporter.GetAtPath(imagePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("Không tìm thấy ảnh hoặc TextureImporter: " + imagePath);
            return false;
        }

        int imageWidth;
        int imageHeight;
        importer.GetSourceTextureWidthAndHeight(out imageWidth, out imageHeight);
        if (imageWidth != expectedWidth || imageHeight != expectedHeight)
        {
            Debug.LogError("Kích thước ảnh đã thay đổi, cần kiểm tra lại trước khi cắt: " + imagePath);
            return false;
        }

        return true;
    }

    private static bool PrepareSheet(string imagePath, string spritePrefix,
        int frameWidth, int frameHeight, int columns, int rows, float pixelsPerUnit)
    {
        TextureImporter importer = AssetImporter.GetAtPath(imagePath) as TextureImporter;
        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null)
        {
            Debug.LogError("Không có Sprite Editor Data Provider. Kiểm tra package 2D Sprite: " + imagePath);
            return false;
        }

        // Thiết lập import qua Unity API, không ghi trực tiếp file .meta.
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.crunchedCompression = false;
        importer.mipmapEnabled = false;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;

        provider.InitSpriteEditorDataProvider();
        ISpriteNameFileIdDataProvider nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameProvider == null)
        {
            Debug.LogError("Không có Sprite Name/File ID Data Provider: " + imagePath);
            return false;
        }

        int imageWidth;
        int imageHeight;
        importer.GetSourceTextureWidthAndHeight(out imageWidth, out imageHeight);
        SpriteRect[] previousRects = provider.GetSpriteRects();
        SpriteRect[] spriteRects = new SpriteRect[columns * rows];
        SpriteNameFileIdPair[] namePairs = new SpriteNameFileIdPair[spriteRects.Length];
        int spriteIndex = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                // Đánh số từ góc trên trái; tọa độ SpriteRect tính từ dưới lên.
                SpriteRect spriteRect = new SpriteRect();
                spriteRect.name = spritePrefix + "_R" + row.ToString("D2") + "_C" + column.ToString("D2");
                spriteRect.rect = new Rect(column * frameWidth,
                    imageHeight - (row + 1) * frameHeight, frameWidth, frameHeight);
                spriteRect.alignment = SpriteAlignment.Center;
                spriteRect.pivot = new Vector2(0.5f, 0.5f);
                spriteRect.spriteID = GUID.Generate();

                // Giữ ID của ô đã có để chạy lại menu không đổi tham chiếu sprite.
                foreach (SpriteRect previousRect in previousRects)
                {
                    if (previousRect.rect == spriteRect.rect)
                    {
                        spriteRect.spriteID = previousRect.spriteID;
                        break;
                    }
                }

                spriteRects[spriteIndex] = spriteRect;
                namePairs[spriteIndex] = new SpriteNameFileIdPair(spriteRect.name, spriteRect.spriteID);
                spriteIndex++;
            }
        }

        provider.SetSpriteRects(spriteRects);
        nameProvider.SetNameFileIdPairs(namePairs);
        provider.Apply();
        importer.SaveAndReimport();
        Debug.Log("Đã chuẩn bị " + spriteRects.Length + " sprite, PPU " + pixelsPerUnit + ": " + imagePath);
        return true;
    }
}
