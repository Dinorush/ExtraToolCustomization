using Gear;
using System;
using System.Collections.Generic;

namespace ExtraToolCustomization.Utils
{
    public static class GearIDRangeExtensions
    {
        private readonly static Dictionary<uint, uint> _checkSumLookup = new();

        public static void CacheOfflineID(GearIDRange range) => range.GetOfflineID();

        public static uint GetOfflineID(this GearIDRange? range)
        {
            if (range == null) return 0;

            if (_checkSumLookup.TryGetValue(range.m_checksum, out var id)) return id;

            if (string.IsNullOrEmpty(range.PlayfabItemInstanceId)) return 0;

            if (uint.TryParse(range.PlayfabItemInstanceId.AsSpan("OfflineGear_ID_".Length), out id))
            {
                _checkSumLookup.TryAdd(range.m_checksum, id);
                return id;
            }

            return 0;
        }
    }
}
