using System.Numerics;
using ImGuiNET;
using SColor = System.Drawing.Color;

namespace DieselExileTools;

public static partial class DXT
{
    public sealed class ColoredText
    {
    private readonly string text;

    public ColoredText(string text) => this.text = text;

    public Vector2 Size => ImGui.CalcTextSize(text);

    public void Draw(ImDrawListPtr drawList, Vector2 position, SColor? defaultColor = null)
    {
        defaultColor ??= SColor.White;
        drawList.AddText(position, defaultColor.Value.ToImGui(), text);
    }
    }
}
