using UnityEditor;

public sealed class CharacterImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Game/Resources/Sprites/")) return;
        var texture = (TextureImporter)assetImporter;
        texture.textureType = TextureImporterType.Default;
        texture.alphaSource = TextureImporterAlphaSource.FromInput;
        texture.alphaIsTransparency = true;
        texture.npotScale = TextureImporterNPOTScale.None;
        texture.mipmapEnabled = false;
        texture.maxTextureSize = 2048;
        texture.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
