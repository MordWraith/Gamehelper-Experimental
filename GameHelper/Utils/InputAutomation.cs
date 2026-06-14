// <copyright file="InputAutomation.cs" company="None">
// Copyright (c) None. All rights reserved.
// </copyright>

namespace GameHelper.Utils
{
    using System.Collections;
    using System.Collections.Generic;
    using ClickableTransparentOverlay.Win32;
    using Coroutine;
    using CoroutineEvents;
    using GameHelper.RemoteEnums;
    using CTOUtils = ClickableTransparentOverlay.Win32.Utils;

    /// <summary>
    ///     Core input automations configured under General - Keys and input.
    /// </summary>
    internal static class InputAutomation
    {
        private const int HideoutHotkeyCooldownMs = 500;

        internal static bool IsChatSequenceRunning => MiscHelper.IsChatSequenceRunning;

        internal static void InitializeCoroutines()
        {
            Core.CoroutinesRegistrar.Add(CoroutineHandler.Start(
                HideoutAutomationCoroutine(),
                "[Input] Hideout automation",
                int.MaxValue - 1));
        }

        private static IEnumerator<Wait> HideoutAutomationCoroutine()
        {
            while (true)
            {
                yield return new Wait(GameHelperEvents.OnRender);

                if (!Core.GHSettings.HideoutAutomationEnabled)
                {
                    continue;
                }

                if (!CTOUtils.IsKeyPressedAndNotTimeout(Core.GHSettings.HideoutAutomationKey, HideoutHotkeyCooldownMs))
                {
                    continue;
                }

                if (!CanSendGameInput())
                {
                    continue;
                }

                MiscHelper.TrySendChatCommand("hideout", "Hideout automation");
            }
        }

        private static bool CanSendGameInput()
        {
            if (Core.GHSettings.EnableControllerMode)
            {
                return false;
            }

            if (Core.States.GameCurrentState != GameStateTypes.InGameState)
            {
                return false;
            }

            if (!Core.Process.Foreground)
            {
                return false;
            }

            if (Core.States.InGameStateObject.GameUi.ChatParent.IsChatActive)
            {
                return false;
            }

            return true;
        }
    }
}
