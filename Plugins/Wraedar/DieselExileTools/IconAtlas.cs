using System.Numerics;
using GameHelper;
using ImGuiNET;
using SColor = System.Drawing.Color;

namespace DieselExileTools;

public static partial class DXT
{
    public sealed class IconAtlas
    {
    private readonly Vector2 iconSize;
    private readonly int columns;
    private readonly int rows;
    private readonly Vector2 textureSize;

    public nint TextureId { get; }

    public IconAtlas(string textureName, string filePath, Vector2 iconSize)
    {
        this.iconSize = iconSize;
        Core.Overlay.AddOrGetImagePointer(filePath, false, out var texture, out var width, out var height);
        TextureId = texture;
        textureSize = new Vector2(width, height);
        columns = Math.Max(1, (int)(width / iconSize.X));
        rows = Math.Max(1, (int)(height / iconSize.Y));
    }

        public DXTRect GetIconUVRect(int index)
        {
            index = Math.Max(0, index);
            var col = index % columns;
            var row = index / columns;
            var uvWidth = iconSize.X / textureSize.X;
            var uvHeight = iconSize.Y / textureSize.Y;
            var topLeft = new Vector2(col * uvWidth, row * uvHeight);
            var bottomRight = topLeft + new Vector2(uvWidth, uvHeight);
            return new DXTRect(topLeft, bottomRight);
        }

        public int IconCount => columns * rows;
    }
}
