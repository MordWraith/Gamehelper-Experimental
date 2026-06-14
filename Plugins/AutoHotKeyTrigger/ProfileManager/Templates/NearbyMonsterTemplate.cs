// <copyright file="NearbyMonsterTemplate.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AutoHotKeyTrigger.ProfileManager.Templates
{
    using AutoHotKeyTrigger.ProfileManager.DynamicConditions;
    using AutoHotKeyTrigger.ProfileManager.DynamicConditions.Interface;
    using GameHelper.Utils;
    using ImGuiNET;
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     ImGui widget that helps user modify the condition code in <see cref="DynamicCondition"/>.
    /// </summary>
    public static class NearbyMonsterTemplate
    {
        private static readonly List<string> SupportedOperatorTypes = new()
        {
            ">",
            ">=",
            "<",
            "<="
        };

        private static string selectedOperator = ">";
        private static bool friendly = false;
        private static int counter = 0;
        private static MonsterRarity selectedRarity = MonsterRarity.Normal;
        private static MonsterNearbyZones zones = MonsterNearbyZones.OuterCircle;

        /// <summary>
        ///     Display the ImGui widget for adding the condition in <see cref="DynamicCondition"/>.
        /// </summary>
        /// <returns>
        ///     condition in string format if user press Add button otherwise empty string.
        /// </returns>
        public static string Add()
        {
            ImGui.Checkbox("Enable friendly monster condition##friendly_nearby_monster_template", ref friendly);
            ImGui.Spacing();

            var countWidth = Math.Max(88f, ImGui.GetFontSize() * 7f);
            var operatorWidth = ImGui.GetFontSize() * 4.5f;
            var fieldWidth = TemplateUi.FieldWidth(0.7f);

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Player has");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(operatorWidth);
            ImGuiHelper.IEnumerableComboBox("##NearbyMonsterOperator", SupportedOperatorTypes, ref selectedOperator);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(countWidth);
            ImGui.InputInt("##NearbyMonsterCount", ref counter, 1, 5);
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(friendly ? "friendly monsters" : "monsters");

            if (!friendly)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Rarity");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(fieldWidth);
                if (ImGui.BeginCombo("##nearby_monster_rarity", $"{selectedRarity}"))
                {
                    foreach (var rarity in Enum.GetValues<MonsterRarity>())
                    {
                        var isSelected = selectedRarity.HasFlag(rarity);
                        if (ImGui.Checkbox($"{rarity}", ref isSelected))
                        {
                            if (isSelected)
                            {
                                selectedRarity |= rarity;
                            }
                            else
                            {
                                selectedRarity &= ~rarity;
                            }
                        }
                    }

                    ImGui.EndCombo();
                }
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Zone");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(fieldWidth);
            ImGuiHelper.EnumComboBox("##NearbyZoneSelector", ref zones);

            if (TemplateUi.AddButton("##NearbyMonsterAdd"))
            {
                if (friendly)
                {
                    return $"{zones}FriendlyMonsterCount {selectedOperator} {counter}";
                }

                if (selectedRarity != 0)
                {
                    return $"MonsterCount(MonsterRarity.{selectedRarity}: MonsterNearbyZones.{zones}) {selectedOperator} {counter}".
                        Replace(", ", "|MonsterRarity.").
                        Replace(":", ",");
                }
            }

            return string.Empty;
        }
    }
}
