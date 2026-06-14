// <copyright file="EndgameAtlasPanelDetector.cs" company="None">
// Copyright (c) None. All rights reserved.
// </copyright>

namespace GameHelper.Utils
{
    using System;
    using GameOffsets.Natives;
    using GameOffsets.Objects.UiElement;

    /// <summary>
    ///     Detects the PoE2 endgame atlas panel via fingerprint backtracking on the GameUi tree.
    ///     Logic mirrors Plugins/Atlas (verified 0.5.x, 2026-06).
    /// </summary>
    internal static class EndgameAtlasPanelDetector
    {
        private const uint AtlasPanelFp = 0x00562EF5;
        private const uint AtlasGateFp = 0x00502EF1;
        private const uint AtlasNodeListFp = 0x00502EF3;
        private const uint IsVisibleMask = 0x800u;
        private const int MaxChildren = 5000;

        public static bool IsOpen(IntPtr gameUiAddress)
        {
            if (gameUiAddress == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                var panelAddr = FindPanelAddress(gameUiAddress);
                if (panelAddr == IntPtr.Zero)
                {
                    return false;
                }

                var reader = Core.Process.Handle;
                var panel = reader.ReadMemory<UiElementBaseOffset>(panelAddr);
                return UiElementBaseFuncs.IsVisibleChecker(panel.Flags);
            }
            catch
            {
                return false;
            }
        }

        private static IntPtr FindPanelAddress(IntPtr gameUiAddress)
        {
            uint[] fps = { AtlasPanelFp, AtlasGateFp, AtlasNodeListFp };
            const int gateStep = 1;
            return WalkFp(gameUiAddress, fps, gateStep, 0);
        }

        private static IntPtr WalkFp(IntPtr parentAddr, uint[] fps, int gateStep, int step)
        {
            if (step == fps.Length)
            {
                return parentAddr;
            }

            var reader = Core.Process.Handle;
            var parent = reader.ReadMemory<UiElementBaseOffset>(parentAddr);
            var childCount = CountChildren(in parent.ChildrensPtr);
            if (childCount <= 0 || childCount > MaxChildren)
            {
                return IntPtr.Zero;
            }

            var target = fps[step] & ~IsVisibleMask;

            for (var pass = 0; pass < 2; pass++)
            {
                var wantVisible = pass == 0;
                for (var i = 0; i < childCount; i++)
                {
                    var childAddr = ReadChildAddress(in parent.ChildrensPtr, i);
                    if (childAddr == IntPtr.Zero)
                    {
                        continue;
                    }

                    var child = reader.ReadMemory<UiElementBaseOffset>(childAddr);
                    var flags = child.Flags;
                    if ((flags & ~IsVisibleMask) != target)
                    {
                        continue;
                    }

                    var visible = (flags & IsVisibleMask) != 0;
                    if (visible != wantVisible)
                    {
                        continue;
                    }

                    if (step == gateStep && !visible)
                    {
                        continue;
                    }

                    var deeper = WalkFp(childAddr, fps, gateStep, step + 1);
                    if (deeper != IntPtr.Zero)
                    {
                        return deeper;
                    }
                }
            }

            return IntPtr.Zero;
        }

        private static int CountChildren(in StdVector vector)
        {
            if (vector.First == IntPtr.Zero || vector.Last == IntPtr.Zero)
            {
                return 0;
            }

            var bytes = vector.Last.ToInt64() - vector.First.ToInt64();
            if (bytes <= 0)
            {
                return 0;
            }

            var stride = IntPtr.Size;
            if ((bytes % stride) != 0)
            {
                return 0;
            }

            var count = bytes / stride;
            if (count <= 0 || count > MaxChildren)
            {
                return 0;
            }

            return (int)count;
        }

        private static IntPtr ReadChildAddress(in StdVector vector, int index)
        {
            var count = CountChildren(in vector);
            if ((uint)index >= (uint)count)
            {
                return IntPtr.Zero;
            }

            var reader = Core.Process.Handle;
            var slot = IntPtr.Add(vector.First, index * IntPtr.Size);
            return reader.ReadMemory<IntPtr>(slot);
        }
    }
}
