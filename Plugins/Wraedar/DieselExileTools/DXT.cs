using System.Collections.Concurrent;
using System.Numerics;
using System.Text.RegularExpressions;
using GameHelper;
using GameHelper.RemoteEnums;
using GameHelper.RemoteObjects.UiElement;
using ImGuiNET;
using SColor = System.Drawing.Color;

namespace DieselExileTools;

public static partial class DXT
{
    private const float LargeMapXBias = 0.6f;
    private const float LargeMapYBias = 0.3f;
    private const float DefaultMapScaleBaseline = 0.187812f;

    private static readonly double CameraAngle = 38.7 * Math.PI / 180;
    private static double diagonalLength;
    private static float mapScale = 0.5f;
    private static float cos;
    private static float sin;

    private static string pluginDirectory = string.Empty;
    private static readonly ConcurrentQueue<Action> deferredActions = new();
    private static readonly Dictionary<string, Dictionary<string, object>> monitorValues = new();
    private static readonly List<FloatingToolbar.Button> toolbarButtons = new();

    public static string PluginName { get; private set; } = string.Empty;
    public static DXTSettings Settings { get; private set; } = new();

    public static Vector2 ActiveMapPosition { get; private set; }
    public static Vector2 ActiveMapSize { get; private set; }
    public static float ActiveMapScale { get; private set; }
    public static Vector2 ActiveMapCenter { get; private set; }
    public static LargeMapUiElement? LargeMap { get; private set; }
    public static bool IsLargeMapVisible { get; private set; }

    public sealed class Config
    {
        public string PluginName { get; set; } = string.Empty;
        public string PluginDirectory { get; set; } = string.Empty;
        public DXTSettings Settings { get; set; } = new();
    }

    public static void Initialise(Config config)
    {
        PluginName = config.PluginName;
        pluginDirectory = config.PluginDirectory;
        Settings = config.Settings;
        if (Settings.MapScaleModifier <= 0f)
        {
            Settings.MapScaleModifier = DefaultMapScaleBaseline;
        }
    }

    public static void AddToolbarButtons(IEnumerable<FloatingToolbar.Button> buttons)
    {
        toolbarButtons.Clear();
        toolbarButtons.AddRange(buttons);
    }

    public static void Tick()
    {
        LargeMap = null;
        IsLargeMapVisible = false;
        ActiveMapPosition = Vector2.Zero;
        ActiveMapSize = new Vector2(Core.Process.WindowArea.Width, Core.Process.WindowArea.Height);

        if (Core.States.GameCurrentState is not (GameStateTypes.InGameState or GameStateTypes.EscapeState))
        {
            return;
        }

        var gameUi = Core.States.InGameStateObject?.GameUi;
        if (gameUi == null)
        {
            return;
        }

        LargeMap = gameUi.LargeMap;
        IsLargeMapVisible = LargeMap.IsVisible && !gameUi.WorldMapPanel.IsVisible;

        if (!IsLargeMapVisible || LargeMap == null)
        {
            return;
        }

        var baseRes = GameOffsets.Objects.UiElement.UiElementBaseFuncs.BaseResolution;
        var baseDiag = Math.Sqrt((baseRes.X * baseRes.X) + (baseRes.Y * baseRes.Y));
        diagonalLength = baseDiag * LargeMap.Size.Y / baseRes.Y;

        var modifier = Settings.MapScaleModifier > 0f ? Settings.MapScaleModifier : DefaultMapScaleBaseline;
        ActiveMapScale = LargeMap.Zoom * modifier;
        mapScale = ActiveMapScale;
        UpdateCosSin();

        var screenCenter = LargeMap.Center + LargeMap.Shift + LargeMap.DefaultShift;
        screenCenter.X += LargeMapXBias;
        screenCenter.Y += LargeMapYBias;
        ActiveMapCenter = screenCenter;
    }

    public static void Render()
    {
        while (deferredActions.TryDequeue(out var action))
        {
            try { action(); }
            catch (Exception ex) { Log($"Deferred action failed: {ex.Message}", false); }
        }

        if (!Settings.ShowTools)
        {
            return;
        }

        FloatingToolbar.Render(toolbarButtons, monitorValues);
    }

    public static Vector2 GridToMap(float x, float y, float z, bool ignoreHeight = false)
    {
        if (!ignoreHeight)
        {
            z /= 10.86957f;
        }
        else
        {
            z = 0f;
        }

        var delta = new Vector2((x - y) * cos, (z - (x + y)) * sin);
        var center = Settings.LargeMapCenterFix
            ? ActiveMapSize * 0.5f
            : ActiveMapCenter - ActiveMapPosition;
        return center + delta;
    }

    public static void Log(string message, bool showInToolbar = true)
    {
        Console.WriteLine($"[{PluginName}] {message}");
        if (showInToolbar)
        {
            Monitor(PluginName, "Log", message);
        }
    }

    public static void Monitor(string group, string key, object value)
    {
        if (!monitorValues.TryGetValue(group, out var groupValues))
        {
            groupValues = new Dictionary<string, object>();
            monitorValues[group] = groupValues;
        }

        groupValues[key] = value;
    }

    public static class Deferred
    {
        public static void Enqueue(Action action) => deferredActions.Enqueue(action);
    }

    private static void UpdateCosSin()
    {
        var scale = 240f / mapScale;
        cos = (float)(diagonalLength * Math.Cos(CameraAngle) / scale);
        sin = (float)(diagonalLength * Math.Sin(CameraAngle) / scale);
    }
}

public static class DXTStringExtensions
{
    public static bool Like(this string input, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return string.IsNullOrEmpty(input);
        }

        if (!pattern.Contains('*') && !pattern.Contains('?'))
        {
            return string.Equals(input, pattern, StringComparison.OrdinalIgnoreCase);
        }

        var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return Regex.IsMatch(input, regex, RegexOptions.IgnoreCase);
    }
}
