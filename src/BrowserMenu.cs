using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HeathenEngineering.SteamworksIntegration;
using Network.Steam;
using TMPro;
using UI.LazyScrollList;
using UI.Menu;
using UnityEngine;

namespace EnusDE.MultiplayerBrowser
{
    public sealed class BrowserMenu : MonoBehaviour
    {
        // Steam permits one lobby list request at a time, even across menu instances.
        private static Task<LobbyData[]> _steamRequest;
        private static float _steamRequestStarted;

        private readonly RefreshSchedule _schedule = new RefreshSchedule();
        private Dictionary<ulong, Entry> _entries = new Dictionary<ulong, Entry>();
        private Entry[] _visible = Array.Empty<Entry>();
        private ClientLobbyHelper _lobbyHelper;
        private Action<LobbyData> _join;
        private TMP_InputField _search;
        private TMP_Text _status;
        private LazyScrollList _list;
        private BrowserControls _controls;
        private string _oldPlaceholder;
        private string _oldSearchText;
        private int _oldLimit;
        private TMP_InputField.CharacterValidation _oldValidation;
        private TMP_InputField.InputType _oldInputType;
        private TMP_InputField.ContentType _oldContentType;
        private TMP_InputField.OnValidateInput _oldValidator;
        private bool _searchConfigured;
        private bool _attached;
        private bool _refreshRequested = true;
        private bool _hadResults;
        private bool _hasRendered;
        private bool _openToNewEmployees;
        private bool _searchChanged;
        private bool _drawNeeded;
        private bool _lastRefreshFailed;
        private int _resultCount;
        private float _markChangedAt;
        private string _searchText = "";
        private string _terms = "";
        private string _requestedMark = "";
        private string _lastQueriedMark;
        private Task<LobbyData[]> _pending;
        private SortOrder _sort;

        internal void Attach(TMP_InputField search, TMP_Text status, LazyScrollList list,
            ClientLobbyHelper lobbyHelper, Action<LobbyData> join)
        {
            if (search == null || status == null || list == null || lobbyHelper == null)
                throw new InvalidOperationException("The multiplayer menu is missing a browser control.");

            _search = search;
            _status = status;
            _list = list;
            _lobbyHelper = lobbyHelper;
            _join = join;

            _oldSearchText = search.text;
            _oldLimit = search.characterLimit;
            _oldValidation = search.characterValidation;
            _oldInputType = search.inputType;
            _oldContentType = search.contentType;
            _oldValidator = search.onValidateInput;
            if (search.placeholder is TMP_Text placeholder)
                _oldPlaceholder = placeholder.text;
            _searchConfigured = true;

            search.contentType = TMP_InputField.ContentType.Standard;
            search.characterLimit = 80;
            search.inputType = TMP_InputField.InputType.Standard;
            search.characterValidation = TMP_InputField.CharacterValidation.None;
            search.onValidateInput = null;
            search.SetTextWithoutNotify("");
            if (search.placeholder is TMP_Text searchHint)
                searchHint.text = "Search railroads";
            search.onValueChanged.AddListener(OnSearchChanged);

            _controls = new BrowserControls(status, ToggleSort, ToggleRestriction);
            _drawNeeded = true;
            _attached = true;
        }

        internal void Detach()
        {
            _attached = false;
            enabled = false;
            _controls?.Dispose();
            _controls = null;
            if (_searchConfigured && _search != null)
            {
                _search.onValueChanged.RemoveListener(OnSearchChanged);
                _search.contentType = _oldContentType;
                _search.characterLimit = _oldLimit;
                _search.inputType = _oldInputType;
                _search.characterValidation = _oldValidation;
                _search.onValidateInput = _oldValidator;
                _search.SetTextWithoutNotify(_oldSearchText);
                if (_search.placeholder is TMP_Text placeholder)
                    placeholder.text = _oldPlaceholder;
            }
            _searchConfigured = false;
        }

        internal void RequestRefresh()
        {
            _refreshRequested = true;
        }

        private void OnSearchChanged(string text)
        {
            _searchText = text;
            _searchChanged = true;
        }

        private void ToggleSort()
        {
            _sort = _sort == SortOrder.Name ? SortOrder.Players : SortOrder.Name;
            _drawNeeded = true;
        }

        private void ToggleRestriction()
        {
            _openToNewEmployees = !_openToNewEmployees;
            _drawNeeded = true;
        }

        private void Update()
        {
            if (!_attached)
                return;

            try
            {
                _controls.UpdateLayout();
                float now = Time.realtimeSinceStartup;
                if (_searchChanged)
                {
                    _searchChanged = false;
                    UpdateSearch(now);
                }
                if (_pending != null)
                    FinishRefresh(now);

                if (_pending == null)
                {
                    bool markChanged = _lastQueriedMark != _requestedMark;
                    bool markReady = now >= _markChangedAt + 0.45f;
                    bool refreshDue = _refreshRequested || (markChanged && markReady)
                        || (!markChanged && now >= _schedule.DueAt);
                    if (refreshDue)
                        StartRefresh(now);
                }

                if (_drawNeeded)
                    Draw();
            }
            catch (Exception exception)
            {
                BrowserMod.RecoverMenu(this, exception);
            }
        }

        private void UpdateSearch(float now)
        {
            string text = _searchText.Trim();
            _terms = text;
            string mark = "";
            if (text.StartsWith("mark:", StringComparison.OrdinalIgnoreCase))
            {
                int end = text.IndexOfAny(new[] { ' ', '\t' }, 5);
                string raw = end == -1 ? text.Substring(5) : text.Substring(5, end - 5);
                mark = ListingText.SanitizeReportingMark(raw);
                _terms = end == -1 ? "" : text.Substring(end + 1);
            }
            if (_requestedMark != mark)
            {
                _requestedMark = mark;
                _markChangedAt = now;
            }
            _drawNeeded = true;
        }

        private void StartRefresh(float now)
        {
            if (_steamRequest != null && !_steamRequest.IsCompleted
                && now - _steamRequestStarted < RefreshSchedule.TimeoutSeconds)
                return;

            // Observe abandoned faults if the previous menu closed during the request.
            if (_steamRequest?.IsFaulted == true)
                _ = _steamRequest.Exception;

            _refreshRequested = false;
            _lastQueriedMark = _requestedMark;
            _steamRequestStarted = now;
            try
            {
                _pending = _steamRequest = _lobbyHelper.FetchLobbies(_lastQueriedMark);
            }
            catch (Exception exception)
            {
                RefreshFailed(now, exception);
            }
            _drawNeeded = true;
        }

        private void FinishRefresh(float now)
        {
            if (!_pending.IsCompleted && now - _steamRequestStarted < RefreshSchedule.TimeoutSeconds)
                return;

            Task<LobbyData[]> finished = _pending;
            _pending = null;
            if (!finished.IsCompleted)
            {
                RefreshFailed(now, new TimeoutException());
            }
            else if (finished.IsFaulted || finished.IsCanceled)
            {
                RefreshFailed(now, finished.Exception?.GetBaseException() ?? new TaskCanceledException());
            }
            else
            {
                // A changed reporting-mark query may complete after the user edits it.
                // Keep the previous snapshot until the requested query completes.
                if (_lastQueriedMark == _requestedMark)
                    StoreResults(finished.Result);
                _lastRefreshFailed = false;
                _schedule.Succeeded(now);
            }
            _drawNeeded = true;
        }

        private void RefreshFailed(float now, Exception exception)
        {
            _lastRefreshFailed = true;
            _schedule.Failed(now);
            BrowserMod.ReportRefreshFailure(exception);
        }

        private void StoreResults(LobbyData[] lobbies)
        {
            var next = new Dictionary<ulong, Entry>();
            foreach (LobbyData lobby in lobbies)
            {
                if (ClientLobbyHelper.ReadListing(lobby, out LobbyListing native) != ListingVerdict.Listed)
                    continue;

                var listing = new Listing(lobby.SteamId.m_SteamID, native.Name, native.Mark,
                    native.Description, native.Version, native.VersionMatches, native.PlayerCount,
                    lobby.MaxMembers, ClientLobbyHelper.NewEmployeeRestriction(lobby));
                if (!_entries.TryGetValue(listing.Id, out Entry entry) || !entry.Listing.SameAs(listing))
                    entry = new Entry(listing, new BrowserLobbyRow(lobby, native, listing, _join));

                next[listing.Id] = entry;
            }
            _entries = next;
            _resultCount = lobbies.Length;
            _hadResults = true;
        }

        private void Draw()
        {
            _drawNeeded = false;
            var matchingMark = _entries.Values.Where(entry => _requestedMark.Length == 0
                || string.Equals(entry.Listing.Mark, _requestedMark, StringComparison.OrdinalIgnoreCase));
            Entry[] visible = BrowserList.Select(matchingMark, entry => entry.Listing, _terms,
                _openToNewEmployees, _sort).ToArray();
            if (!_hasRendered || !_visible.SequenceEqual(visible))
            {
                _list.SetData(visible.Select(entry => (object)entry.Row).ToList());
                _visible = visible;
                _hasRendered = true;
            }

            string status;
            if (_pending != null || (!_hadResults && _refreshRequested))
                status = _hadResults ? "Refreshing…" : "Finding railroads…";
            else if (_lastRefreshFailed)
                status = _hadResults ? "Steam error · Cached list" : "Steam error · Retrying";
            else
            {
                status = visible.Length == 0 ? "No railroads found." : $"{visible.Length} of {_entries.Count} railroads";
                if (_resultCount >= ClientLobbyHelper.MaxLobbyResults)
                    status += " · Steam limit";
            }

            _status.text = status;
            _controls.UpdateLabels(_sort, _openToNewEmployees);
        }

        private void OnDestroy()
        {
            Detach();
        }

        private sealed class Entry
        {
            internal Entry(Listing listing, LobbyRow.Info row)
            {
                Listing = listing;
                Row = row;
            }

            internal Listing Listing { get; }
            internal LobbyRow.Info Row { get; }
        }
    }
}
