using System;
using System.Reflection;
using HarmonyLib;
using HeathenEngineering.SteamworksIntegration;
using Network.Steam;
using Railroader.Modding;
using TMPro;
using UI.LazyScrollList;
using UI.Menu;
using UnityEngine;

namespace EnusDE.MultiplayerBrowser
{
    public sealed class BrowserMod : IModEntryPoint
    {
        private const string PatchId = "enusde.multiplayer-browser";
        private Harmony _harmony;
        private static IModLogger _log;
        private static bool _enabled;
        private static FieldInfo _searchField;
        private static FieldInfo _statusLabel;
        private static FieldInfo _scrollList;
        private static FieldInfo _lobbyHelper;
        private static FieldInfo _rowDetails;
        private static FieldInfo _rowCount;
        private static bool _rowErrorLogged;
        private static MethodInfo _join;
        private static MethodInfo _refresh;

        public void OnModLoad(IModContext context)
        {
            _log = context.Log;
            try
            {
                Type type = typeof(MultiplayerJoinMenu);
                _searchField = RequireField(type, "filterField", typeof(TMP_InputField));
                _statusLabel = RequireField(type, "statusLabel", typeof(TMP_Text));
                _scrollList = RequireField(type, "scrollList", typeof(LazyScrollList));
                _lobbyHelper = RequireField(type, "_clientLobbyHelper", typeof(ClientLobbyHelper));
                _join = RequireMethod(type, "JoinLobbyById", typeof(LobbyData));
                _rowDetails = RequireField(typeof(LobbyRow), "detailsLabel", typeof(TMP_Text));
                _rowCount = RequireField(typeof(LobbyRow), "countLabel", typeof(TMP_Text));
                _rowErrorLogged = false;
                _refresh = RequireMethod(type, "RefreshLobbies");
                var searchChanged = RequireMethod(type, nameof(MultiplayerJoinMenu.SearchFieldChanged));
                var prefix = AccessTools.DeclaredMethod(typeof(BrowserMod), nameof(RefreshPrefix));
                _harmony = new Harmony(PatchId);
                _harmony.Patch(_refresh, prefix: new HarmonyMethod(prefix));
                var searchPrefix = AccessTools.DeclaredMethod(typeof(BrowserMod), nameof(SearchChangedPrefix));
                _harmony.Patch(searchChanged, prefix: new HarmonyMethod(searchPrefix));
                var configureRow = RequireMethod(typeof(LobbyRow), nameof(LobbyRow.Configure), typeof(int), typeof(object));
                var rowPostfix = AccessTools.DeclaredMethod(typeof(BrowserMod), nameof(ConfigureRowPostfix));
                _harmony.Patch(configureRow, postfix: new HarmonyMethod(rowPostfix));
                _enabled = true;
                _log.Info("Multiplayer Browser is ready for Join Multiplayer.");
            }
            catch (Exception exception)
            {
                _enabled = false;
                _log.Error(exception, "Could not enable the Multiplayer Browser. Railroader's menu is available.");
                _harmony?.UnpatchAll(PatchId);
                _harmony = null;
            }
        }

        public void OnModUnload()
        {
            _enabled = false;
            try
            {
                _harmony?.UnpatchAll(PatchId);
                foreach (var browser in UnityEngine.Object.FindObjectsByType<BrowserMenu>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    bool wasActive = browser.isActiveAndEnabled;
                    var menu = browser.GetComponent<MultiplayerJoinMenu>();
                    browser.Detach();
                    UnityEngine.Object.Destroy(browser);
                    if (wasActive && menu != null)
                        _refresh.Invoke(menu, null);
                }
            }
            finally
            {
                _harmony = null;
                _log = null;
            }
        }

        private static bool RefreshPrefix(MultiplayerJoinMenu __instance)
        {
            if (!_enabled || !__instance.isActiveAndEnabled)
                return true;

            BrowserMenu browser = __instance.GetComponent<BrowserMenu>();
            try
            {
                if (browser == null)
                {
                    browser = __instance.gameObject.AddComponent<BrowserMenu>();
                    var join = (Action<LobbyData>)_join.CreateDelegate(typeof(Action<LobbyData>), __instance);
                    browser.Attach(
                        (TMP_InputField)_searchField.GetValue(__instance),
                        (TMP_Text)_statusLabel.GetValue(__instance),
                        (LazyScrollList)_scrollList.GetValue(__instance),
                        (ClientLobbyHelper)_lobbyHelper.GetValue(__instance),
                        join);
                }

                if (!browser.isActiveAndEnabled)
                    return true;

                browser.RequestRefresh();
                return false;
            }
            catch (Exception exception)
            {
                browser?.Detach();
                _log?.Error(exception, "Could not extend Join Multiplayer. Using Railroader's menu.");
                return true;
            }
        }

        private static bool SearchChangedPrefix(MultiplayerJoinMenu __instance)
        {
            if (!_enabled)
                return true;
            var browser = __instance.GetComponent<BrowserMenu>();
            return browser == null || !browser.isActiveAndEnabled;
        }

        private static void ConfigureRowPostfix(LobbyRow __instance, object __1)
        {
            if (!_enabled || !(__1 is BrowserLobbyRow row))
                return;
            try
            {
                row.AddDetails((TMP_Text)_rowDetails.GetValue(__instance), (TMP_Text)_rowCount.GetValue(__instance));
            }
            catch (Exception exception)
            {
                if (!_rowErrorLogged)
                {
                    _rowErrorLogged = true;
                    _log?.Error(exception, "Could not show additional lobby details.");
                }
            }
        }

        internal static void RecoverMenu(BrowserMenu browser, Exception exception)
        {
            var menu = browser.GetComponent<MultiplayerJoinMenu>();
            browser.Detach();
            _log?.Error(exception, "Could not update the browser UI. Using Railroader's menu.");
            if (_enabled && menu != null && menu.isActiveAndEnabled)
                _refresh.Invoke(menu, null);
        }

        internal static void ReportRefreshFailure(Exception exception)
        {
            // Avoid copying Steam IDs or lobby text from exception messages into our log.
            _log?.Warning("Steam lobby refresh failed (" + exception.GetType().Name + ").");
        }

        private static FieldInfo RequireField(Type type, string name, Type fieldType)
        {
            var field = AccessTools.DeclaredField(type, name);
            if (field?.FieldType != fieldType)
                throw new MissingFieldException(type.FullName, name);
            return field;
        }

        private static MethodInfo RequireMethod(Type type, string name, params Type[] parameters)
        {
            return AccessTools.DeclaredMethod(type, name, parameters)
                ?? throw new MissingMethodException(type.FullName, name);
        }
    }
}
