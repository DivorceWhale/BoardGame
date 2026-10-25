using UnityEditor;
using UnityEngine;

// Gives the six block cards their card-face art (block shape sprites), so they render
// through the same CardUI prefab as the event cards.
// Run from: QUAKER > Build 6 > Set Up Block Cards.
public static class Build6BlockCards
{
    static readonly string[,] Pairs =
    {
        { "Assets/Prefabs/Cards/Blocks/The Cube.asset",   "Assets/Art/BlockArt/Block_TheCube.png" },
        { "Assets/Prefabs/Cards/Blocks/Long Block.asset", "Assets/Art/BlockArt/Block_LongBlock.png" },
        { "Assets/Prefabs/Cards/Blocks/L-Block.asset",    "Assets/Art/BlockArt/Block_LBlock.png" },
        { "Assets/Prefabs/Cards/Blocks/BL-Block.asset",   "Assets/Art/BlockArt/Block_BLBlock.png" },
        { "Assets/Prefabs/Cards/Blocks/T-Block.asset",    "Assets/Art/BlockArt/Block_TBlock.png" },
        { "Assets/Prefabs/Cards/Blocks/Z-Block.asset",    "Assets/Art/BlockArt/Block_ZBlock.png" },
    };

    [MenuItem("QUAKER/Build 6/Set Up Block Cards")]
    public static void SetUp()
    {
        int done = 0;
        for (int i = 0; i < Pairs.GetLength(0); i++)
        {
            string cardPath = Pairs[i, 0];
            string artPath = Pairs[i, 1];

            TextureImporter ti = AssetImporter.GetAtPath(artPath) as TextureImporter;
            if (ti == null) { Debug.LogWarning("Build 6: missing " + artPath); continue; }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();

            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(cardPath);
            Sprite art = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
            if (card == null || art == null) { Debug.LogWarning("Build 6: could not set art for " + cardPath); continue; }

            card.art = art;
            card.weight = EventWeight.None;
            card.cardType = CardType.Block;
            EditorUtility.SetDirty(card);
            done++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Build 6: block card art set on " + done + " of 6 cards.");
    }
}
