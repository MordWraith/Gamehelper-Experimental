namespace FarmTracker
{
    using System;

    /// <summary>Heuristics for in-map side zones that should share one FarmTracker run.</summary>
    internal static class FarmMapSubAreaRules
    {
        internal static bool IsSubArea(string areaId)
        {
            if (string.IsNullOrEmpty(areaId))
            {
                return false;
            }

            return areaId.StartsWith("Abyss_", StringComparison.Ordinal)
                || areaId.StartsWith("ExpeditionSubArea_", StringComparison.Ordinal)
                || areaId.StartsWith("BreachDomain_", StringComparison.Ordinal);
        }
    }
}
