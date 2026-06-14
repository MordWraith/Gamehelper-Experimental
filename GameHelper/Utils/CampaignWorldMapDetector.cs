// <copyright file="CampaignWorldMapDetector.cs" company="None">
// Copyright (c) None. All rights reserved.
// </copyright>

namespace GameHelper.Utils
{
    using GameHelper.RemoteObjects.States.InGameStateObjects;

    /// <summary>
    ///     Detects the campaign WORLD / checkpoint fullscreen map (not the in-area Tab overlay).
    ///     Uses the full parent-chain visibility check — raw flags on WorldMapPanel stay set in memory.
    /// </summary>
    internal static class CampaignWorldMapDetector
    {
        public static bool IsOpen(ImportantUiElements gameUi)
        {
            if (gameUi.WorldMapPanel.Address == System.IntPtr.Zero)
            {
                return false;
            }

            return gameUi.WorldMapPanel.IsVisible;
        }
    }
}
