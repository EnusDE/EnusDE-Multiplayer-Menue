using System;
using HeathenEngineering.SteamworksIntegration;
using Network.Steam;
using TMPro;
using UI.Builder;
using UI.Menu;
using UnityEngine;

namespace EnusDE.MultiplayerBrowser
{
    internal sealed class BrowserLobbyRow : LobbyRow.Info
    {
        private readonly Listing _listing;

        internal BrowserLobbyRow(LobbyData lobby, LobbyListing native, Listing listing, Action<LobbyData> join)
            : base(lobby, native, listing.Restriction, join)
        {
            _listing = listing;
        }

        internal void AddDetails(TMP_Text details, TMP_Text count)
        {
            // Railroader already draws version warnings in the original row.
            string version = _listing.VersionMatches ? $"Railroader {Application.version}" : "";
            string capacity = _listing.Capacity > 0 ? $"Capacity {_listing.Capacity}" : "";
            if (HasDetails)
                details.text = Combine(Combine(version, capacity), Details);

            string tooltip = Combine(version, capacity);
            if (tooltip.Length != 0)
                count.rectTransform.Tooltip("Players", Combine($"{_listing.Players} connected", tooltip));
        }

        private static string Combine(string first, string second)
        {
            if (string.IsNullOrEmpty(first))
                return second;
            return string.IsNullOrEmpty(second) ? first : first + " · " + second;
        }
    }
}
