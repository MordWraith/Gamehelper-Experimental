namespace AutoHotKeyTrigger.ProfileManager.Templates
{
    using System;
    using System.Numerics;
    using ImGuiNET;

    /// <summary>
    ///     Shared layout helpers for condition template dialogs.
    /// </summary>
    internal static class TemplateUi
    {
        internal const float PopupMinWidth = 360f;
        internal const float PopupDefaultWidth = 520f;
        internal const float PopupDefaultHeight = 300f;

        internal static void PrepareConditionDialog()
        {
            ImGui.SetNextWindowSize(
                new Vector2(PopupDefaultWidth, PopupDefaultHeight),
                ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(
                new Vector2(PopupMinWidth, 160f),
                new Vector2(900f, 700f));
        }

        internal static float ContentWidth()
        {
            var avail = ImGui.GetContentRegionAvail().X;
            if (avail > 1f)
            {
                return avail;
            }

            var padding = ImGui.GetStyle().WindowPadding.X * 2f;
            return PopupDefaultWidth - padding;
        }

        internal static float FieldWidth(float fraction = 1f)
        {
            return ContentWidth() * Math.Clamp(fraction, 0.1f, 1f);
        }

        internal static bool AddButton(string id = "##TemplateAdd")
        {
            return ImGui.Button($"Add{id}", new Vector2(Math.Max(80f, ImGui.GetFontSize() * 5f), 0));
        }
    }
}
