using System.Drawing;
using System.Numerics;
using ImGuiNET;

namespace DieselExileTools;

public static partial class DXT
{
    public static class Color
    {
        public static System.Drawing.Color FromRGBA(byte r, byte g, byte b, byte a = 255)
            => System.Drawing.Color.FromArgb(a, r, g, b);

        public static System.Drawing.Color FromRGBA(int r, int g, int b, int a = 255)
            => FromRGBA((byte)r, (byte)g, (byte)b, (byte)a);

        public static System.Drawing.Color FromHsla(int h, int s, int l, int a = 255)
        {
            var hue = h / 255f * 360f;
            var sat = s / 255f;
            var lum = l / 255f;
            return FromHsl(hue, sat, lum, a / 255f);
        }

        private static System.Drawing.Color FromHsl(float h, float s, float l, float a)
        {
            h = (h % 360f + 360f) % 360f;
            var c = (1f - MathF.Abs(2f * l - 1f)) * s;
            var x = c * (1f - MathF.Abs((h / 60f % 2f) - 1f));
            var m = l - c / 2f;
            float r, g, b;
            if (h < 60f) (r, g, b) = (c, x, 0);
            else if (h < 120f) (r, g, b) = (x, c, 0);
            else if (h < 180f) (r, g, b) = (0, c, x);
            else if (h < 240f) (r, g, b) = (0, x, c);
            else if (h < 300f) (r, g, b) = (x, 0, c);
            else (r, g, b) = (c, 0, x);
            return System.Drawing.Color.FromArgb(
                (int)(a * 255f),
                (int)((r + m) * 255f),
                (int)((g + m) * 255f),
                (int)((b + m) * 255f));
        }
    }

    public static class Colors
    {
        public static System.Drawing.Color Button => Color.FromRGBA(51, 65, 85, 255);
        public static System.Drawing.Color ButtonActive => Color.FromRGBA(59, 130, 246, 255);
        public static System.Drawing.Color ButtonActiveHover => Color.FromRGBA(96, 165, 250, 255);
        public static System.Drawing.Color Text => Color.FromRGBA(226, 232, 240, 255);
        public static System.Drawing.Color TextRed => Color.FromRGBA(248, 113, 113, 255);
        public static System.Drawing.Color TextYellow => Color.FromRGBA(250, 204, 21, 255);
        public static System.Drawing.Color TextGreen => Color.FromRGBA(74, 222, 128, 255);
        public static System.Drawing.Color ControlRed => Color.FromRGBA(220, 38, 38, 255);
        public static System.Drawing.Color TextOnColor => Color.FromRGBA(255, 255, 255, 255);
    }

    public static class Palettes
    {
        public static class Tailwind
        {
            public static TailwindPalette Slate800 => new(30, 41, 59, 255);
            public static TailwindPalette Slate600 => new(71, 85, 105, 255);
            public static TailwindPalette Slate500 => new(100, 116, 139, 255);
        }
    }

    public readonly struct TailwindPalette
    {
        public System.Drawing.Color Color { get; }
        public TailwindPalette(byte r, byte g, byte b, byte a) => Color = System.Drawing.Color.FromArgb(a, r, g, b);
    }
}

public static class DXTColorExtensions
{
    public static uint ToImGui(this System.Drawing.Color color)
        => ImGui.ColorConvertFloat4ToU32(new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f));

    public static Vector4 ToVector4(this System.Drawing.Color color)
        => new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
}
