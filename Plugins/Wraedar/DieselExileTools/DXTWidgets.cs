using System.Numerics;
using ImGuiNET;
using SColor = System.Drawing.Color;

namespace DieselExileTools;

public static partial class DXT
{
    public static class Tooltip
    {
        public abstract class Line;

        public sealed class Title : Line
        {
            public string Text { get; set; } = string.Empty;
        }

        public sealed class Description : Line
        {
            public string Text { get; set; } = string.Empty;
        }

        public sealed class Separator : Line;

        public sealed class DoubleLine : Line
        {
            public string LeftText { get; set; } = string.Empty;
            public string RightText { get; set; } = string.Empty;
        }

        public sealed class Options
        {
            public List<Line> Lines { get; set; } = new();

            public Options()
            {
            }

            public Options(string title)
            {
                Lines.Add(new Title { Text = title });
            }

            public Options(string title, string description)
            {
                Lines.Add(new Title { Text = title });
                Lines.Add(new Description { Text = description });
            }
        }

        public static void Draw(Options options)
        {
            ImGui.BeginTooltip();
            foreach (var line in options.Lines)
            {
                switch (line)
                {
                    case Title title:
                        ImGui.TextUnformatted(title.Text);
                        break;
                    case Description description:
                        ImGui.TextWrapped(description.Text);
                        break;
                    case Separator:
                        ImGui.Separator();
                        break;
                    case DoubleLine doubleLine:
                        ImGui.TextUnformatted($"{doubleLine.LeftText} {doubleLine.RightText}");
                        break;
                }
            }

            ImGui.EndTooltip();
        }

        public static void Hover(Options? options)
        {
            if (options == null || !ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                return;
            }

            Draw(options);
        }
    }

    public static class Panel
    {
        public sealed class Options
        {
            public float Width { get; set; } = -1f;
        }

        public static void Begin(string id, Options options)
        {
            var width = options.Width <= 0 ? ImGui.GetContentRegionAvail().X : options.Width;
            ImGui.BeginChild(id, new Vector2(width, 0), ImGuiChildFlags.None);
        }

        public static void End(string id) => ImGui.EndChild();
    }

    public static class CollapsingPanel
    {
        public sealed class Options
        {
            public string Label { get; set; } = string.Empty;
            public float Width { get; set; } = -1f;
        }

        private static int openDepth;

        public static bool Begin(string id, ref bool collapsed, Options options)
        {
            ImGui.PushID(id);
            var open = !collapsed;
            var flags = ImGuiTreeNodeFlags.DefaultOpen;
            if (ImGui.CollapsingHeader(options.Label, ref open, flags))
            {
                collapsed = !open;
                openDepth++;
                return true;
            }

            collapsed = !open;
            ImGui.PopID();
            return false;
        }

        public static void End(string id)
        {
            if (openDepth <= 0)
            {
                return;
            }

            openDepth--;
            ImGui.PopID();
        }
    }

    public static class Window
    {
        public sealed class Options
        {
            public string Title { get; set; } = string.Empty;
            public float Width { get; set; } = 400f;
            public float Height { get; set; } = 300f;
            public bool Resizable { get; set; } = true;
            public float MinWidth { get; set; }
            public int LockHeight { get; set; }
            public int TitleBarHeight { get; set; }
        }

        public static bool Begin(string id, ref bool open, Options options)
        {
            if (options.LockHeight > 0)
            {
                var maxHeight = ImGui.GetIO().DisplaySize.Y * 0.85f;
                var height = Math.Min(options.LockHeight, maxHeight);
                ImGui.SetNextWindowSize(new Vector2(options.Width > 0 ? options.Width : 400f, height), ImGuiCond.Always);
            }
            else
            {
                ImGui.SetNextWindowSize(new Vector2(options.Width, options.Height), ImGuiCond.FirstUseEver);
                var maxHeight = ImGui.GetIO().DisplaySize.Y * 0.9f;
                var minWidth = options.MinWidth > 0 ? options.MinWidth : 320f;
                ImGui.SetNextWindowSizeConstraints(new Vector2(minWidth, 200f), new Vector2(float.MaxValue, maxHeight));
            }

            if (options.MinWidth > 0 && options.LockHeight > 0)
            {
                ImGui.SetNextWindowSizeConstraints(new Vector2(options.MinWidth, 200f), new Vector2(float.MaxValue, ImGui.GetIO().DisplaySize.Y * 0.9f));
            }

            var flags = ImGuiWindowFlags.NoCollapse;
            if (!options.Resizable)
            {
                flags |= ImGuiWindowFlags.NoResize;
            }

            if (ImGui.Begin($"{options.Title}##{id}", ref open, flags))
            {
                options.TitleBarHeight = (int)(ImGui.GetFrameHeight() + ImGui.GetStyle().WindowPadding.Y);
                return true;
            }

            return false;
        }

        public static void End() => ImGui.End();
    }

    public static class Checkbox
    {
        public sealed class Options
        {
            public float Width { get; set; }
            public float Height { get; set; }
            public Tooltip.Options? Tooltip { get; set; }
        }

        public static void Draw(string id, ref bool value, Options? options = null)
        {
            ImGui.PushID(id);
            if (options?.Height > 0)
            {
                ImGui.SetNextItemWidth(options.Height);
            }

            ImGui.Checkbox("##cb", ref value);
            Tooltip.Hover(options?.Tooltip);
            ImGui.PopID();
        }
    }

    public static class Label
    {
        public sealed class Options
        {
            public float Width { get; set; } = -1f;
            public float Height { get; set; }
            public SColor? TextColor { get; set; }
            public bool DrawBG { get; set; }
            public float PadLeft { get; set; }
            public Tooltip.Options? Tooltip { get; set; }
        }

        public static void Draw(string text, Options? options = null)
        {
            options ??= new Options();
            var textSize = ImGui.CalcTextSize(text);
            var height = options.Height > 0 ? options.Height : ImGui.GetFrameHeight();
            var width = options.Width > 0 ? options.Width : textSize.X + options.PadLeft + 4f;
            var pos = ImGui.GetCursorScreenPos();
            var size = new Vector2(width, height);

            if (options.DrawBG)
            {
                ImGui.GetWindowDrawList().AddRectFilled(
                    pos,
                    pos + size,
                    SColor.FromArgb(40, 0, 0, 0).ToImGui());
            }

            var textColor = options.TextColor?.ToImGui() ?? ImGui.GetColorU32(ImGuiCol.Text);
            var textY = pos.Y + Math.Max(0f, (height - textSize.Y) * 0.5f);
            ImGui.GetWindowDrawList().AddText(new Vector2(pos.X + options.PadLeft, textY), textColor, text);

            ImGui.SetCursorScreenPos(pos);
            ImGui.InvisibleButton($"{text}##label", size);
            Tooltip.Hover(options.Tooltip);
            ImGui.SetCursorScreenPos(pos + new Vector2(0f, size.Y));
        }
    }

    public static class ColorSelect
    {
        public sealed class Options
        {
            public float Width { get; set; } = ImGui.GetFrameHeight();
            public float Height { get; set; } = ImGui.GetFrameHeight();
        }

        public static void Draw(string id, string label, ref SColor color, Options? options = null)
        {
            options ??= new Options();
            ImGui.PushID(id);
            var vec = color.ToVector4();
            ImGui.ColorEdit4($"##{label}", ref vec, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview);
            color = SColor.FromArgb(
                (int)(vec.W * 255f),
                (int)(vec.X * 255f),
                (int)(vec.Y * 255f),
                (int)(vec.Z * 255f));
            ImGui.PopID();
        }
    }

    public static class Button
    {
        public sealed class Options
        {
            public string Label { get; set; } = string.Empty;
            public float Width { get; set; } = 80f;
            public float Height { get; set; } = 22f;
            public bool Enabled { get; set; } = true;
            public SColor? Color { get; set; }
            public SColor? TextColor { get; set; }
            public Tooltip.Options? Tooltip { get; set; }
        }

        public static bool Draw(string id, Options options)
        {
            ImGui.PushID(id);
            if (!options.Enabled)
            {
                ImGui.BeginDisabled();
            }

            var pushed = 0;
            if (options.Color.HasValue)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, options.Color.Value.ToImGui());
                pushed++;
            }

            if (options.TextColor.HasValue)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, options.TextColor.Value.ToImGui());
                pushed++;
            }

            var clicked = ImGui.Button(options.Label, new Vector2(options.Width, options.Height));
            Tooltip.Hover(options.Tooltip);

            if (pushed > 0)
            {
                ImGui.PopStyleColor(pushed);
            }

            if (!options.Enabled)
            {
                ImGui.EndDisabled();
            }

            ImGui.PopID();
            return clicked;
        }

        public static bool Draw(string id, ref bool value, Options options)
        {
            return DrawToggle(id, ref value, options);
        }

        public static bool DrawToggle(string id, ref bool value, Options options)
        {
            ImGui.PushID(id);
            if (!options.Enabled)
            {
                ImGui.BeginDisabled();
            }

            var pushed = 0;
            if (value)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, Colors.ButtonActive.ToImGui());
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Colors.ButtonActiveHover.ToImGui());
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, Colors.ButtonActive.ToImGui());
                pushed = 3;
            }
            else if (options.Color.HasValue)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, options.Color.Value.ToImGui());
                pushed++;
            }

            if (options.TextColor.HasValue)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, options.TextColor.Value.ToImGui());
                pushed++;
            }

            var clicked = ImGui.Button(options.Label, new Vector2(options.Width, options.Height));
            Tooltip.Hover(options.Tooltip);

            if (pushed > 0)
            {
                ImGui.PopStyleColor(pushed);
            }

            if (!options.Enabled)
            {
                ImGui.EndDisabled();
            }

            if (clicked)
            {
                value = !value;
            }

            ImGui.PopID();
            return clicked;
        }
    }

    public static class Slider
    {
        public sealed class Options
        {
            public float Width { get; set; } = 100f;
            public float Height { get; set; }
            public float Min { get; set; }
            public float Max { get; set; } = 100f;
            public Tooltip.Options? Tooltip { get; set; }
        }

        public static void Draw(string id, ref int value, Options? options = null)
        {
            options ??= new Options();
            ImGui.PushID(id);
            var f = (float)value;
            ImGui.SetNextItemWidth(options.Width);
            if (ImGui.SliderFloat("##slider", ref f, options.Min, options.Max))
            {
                value = (int)Math.Round(f);
            }

            Tooltip.Hover(options?.Tooltip);
            ImGui.PopID();
        }

        public static void Draw(string id, ref float value, Options? options = null)
        {
            options ??= new Options();
            ImGui.PushID(id);
            ImGui.SetNextItemWidth(options.Width);
            ImGui.SliderFloat("##slider", ref value, options.Min, options.Max);
            Tooltip.Hover(options?.Tooltip);
            ImGui.PopID();
        }
    }

    public static class Input
    {
        public sealed class Options
        {
            public float Width { get; set; } = 120f;
            public float Height { get; set; }
            public SColor? BackgroundColor { get; set; }
            public SColor? TextColor { get; set; }
            public Tooltip.Options? Tooltip { get; set; }
        }

        public static void Draw(string id, ref string value, Options? options = null)
        {
            options ??= new Options();
            ImGui.PushID(id);
            ImGui.SetNextItemWidth(options.Width);
            ImGui.InputText("##input", ref value, 512);
            Tooltip.Hover(options.Tooltip);
            ImGui.PopID();
        }
    }

    public static class Select
    {
        public sealed class Options
        {
            public float Width { get; set; } = 120f;
            public float Height { get; set; }
            public List<string> Items { get; set; } = new();
            public Tooltip.Options? Tooltip { get; set; }
        }

        public static bool Draw(string id, ref int selectedIndex, Options options)
        {
            ImGui.PushID(id);
            var changed = false;
            var preview = selectedIndex >= 0 && selectedIndex < options.Items.Count
                ? options.Items[selectedIndex]
                : string.Empty;
            ImGui.SetNextItemWidth(options.Width);
            if (ImGui.BeginCombo("##select", preview))
            {
                for (var i = 0; i < options.Items.Count; i++)
                {
                    var isSelected = i == selectedIndex;
                    if (ImGui.Selectable(options.Items[i], isSelected))
                    {
                        selectedIndex = i;
                        changed = true;
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
            }

            Tooltip.Hover(options.Tooltip);
            ImGui.PopID();
            return changed;
        }
    }

    public static class Menu
    {
        public sealed class Item
        {
            public string Label { get; set; } = string.Empty;
            public string RightLabel { get; set; } = string.Empty;
            public bool Separator { get; set; }
            public bool Enabled { get; set; } = true;
            public Tooltip.Options? Tooltip { get; set; }
            public Action? OnClick { get; set; }
        }

        private static readonly Dictionary<string, List<Item>> OpenMenus = new();

        public static void Open(string id, List<Item> items)
        {
            OpenMenus[id] = items;
            ImGui.OpenPopup(id);
        }

        public static void Draw(string id)
        {
            if (!OpenMenus.TryGetValue(id, out var items))
            {
                return;
            }

            if (ImGui.BeginPopup(id))
            {
                foreach (var item in items)
                {
                    if (item.Separator)
                    {
                        ImGui.Separator();
                        continue;
                    }

                    if (!item.Enabled)
                    {
                        ImGui.BeginDisabled();
                    }

                    if (ImGui.MenuItem(item.Label, item.RightLabel))
                    {
                        item.OnClick?.Invoke();
                    }

                    Tooltip.Hover(item.Tooltip);

                    if (!item.Enabled)
                    {
                        ImGui.EndDisabled();
                    }
                }

                ImGui.EndPopup();
                OpenMenus.Remove(id);
            }
        }
    }

    public static class TextInputModal
    {
        public sealed class Options
        {
            public string Title { get; set; } = string.Empty;
            public Tooltip.Options? Tooltip { get; set; }
        }

        private sealed class State
        {
            public bool Open;
            public string Value = string.Empty;
            public Options Options = new();
        }

        private static readonly Dictionary<string, State> States = new();

        public static void Open(string id, string defaultValue, Options? options = null)
        {
            States[id] = new State
            {
                Open = true,
                Value = defaultValue,
                Options = options ?? new Options(),
            };
            ImGui.OpenPopup(id);
        }

        public static (bool ok, string? value) Draw(string id)
        {
            if (!States.TryGetValue(id, out var state) || !state.Open)
            {
                return (false, null);
            }

            var ok = false;
            ImGui.SetNextWindowSize(new Vector2(320, 0), ImGuiCond.Always);
            if (ImGui.BeginPopupModal(state.Options.Title, ref state.Open, ImGuiWindowFlags.AlwaysAutoResize))
            {
                Tooltip.Hover(state.Options.Tooltip);
                ImGui.InputText("##value", ref state.Value, 256);
                if (ImGui.Button("OK"))
                {
                    ok = true;
                    state.Open = false;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    state.Open = false;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            if (!state.Open)
            {
                var value = ok ? state.Value : null;
                States.Remove(id);
                return (ok, value);
            }

            return (false, null);
        }
    }

    public static class IconSelect
    {
        public sealed class Options
        {
            public float Width { get; set; } = ImGui.GetFrameHeight();
            public float Height { get; set; } = ImGui.GetFrameHeight();
            public SColor IconColor { get; set; } = SColor.White;
        }

        public static void Draw(string id, string label, ref int index, IconAtlas atlas, Options? options = null)
        {
            options ??= new Options();
            ImGui.PushID(id);
            var uv = atlas.GetIconUVRect(index);
            var size = new Vector2(options.Width, options.Height);
            var tintVec = options.IconColor.ToVector4();
            if (ImGui.ImageButton(label, atlas.TextureId, size, uv.TopLeft, uv.BottomRight, Vector4.Zero, tintVec))
            {
                index = (index + 1) % Math.Max(1, atlas.IconCount);
            }

            ImGui.PopID();
        }
    }

    public static class Keyboard
    {
        public enum Keys
        {
            ControlKey,
        }

        public static bool IsKeyDown(Keys key)
        {
            return key switch
            {
                Keys.ControlKey => ImGui.GetIO().KeyCtrl,
                _ => false,
            };
        }
    }

    public static class Mouse
    {
        public static bool IsLeftButtonDown() => ImGui.IsMouseDown(ImGuiMouseButton.Left);
    }

    public static class FloatingToolbar
    {
        public sealed class Button
        {
            public string Label { get; set; } = string.Empty;
            public Tooltip.Options? Tooltip { get; set; }
            public Func<bool>? GetChecked { get; set; }
            public Action<bool>? SetChecked { get; set; }
        }

        public static void Render(List<Button> buttons, Dictionary<string, Dictionary<string, object>> monitors)
        {
            ImGui.SetNextWindowBgAlpha(0.85f);
            if (!ImGui.Begin($"{DXT.PluginName} Debug", ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.End();
                return;
            }

            foreach (var button in buttons)
            {
                var checkedState = button.GetChecked?.Invoke() ?? false;
                if (ImGui.Checkbox(button.Label, ref checkedState))
                {
                    button.SetChecked?.Invoke(checkedState);
                }

                Tooltip.Hover(button.Tooltip);
            }

            ImGui.Separator();
            foreach (var (group, values) in monitors)
            {
                ImGui.TextUnformatted(group);
                foreach (var (key, value) in values)
                {
                    ImGui.BulletText($"{key}: {value}");
                }
            }

            ImGui.End();
        }
    }
}
