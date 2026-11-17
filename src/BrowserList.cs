using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.MultiplayerBrowser
{
    internal enum SortOrder
    {
        Name,
        Players
    }

    internal sealed class Listing
    {
        internal Listing(ulong id, string name, string mark, string description, string version,
            bool versionMatches, int players, int capacity, string restriction)
        {
            Id = id;
            Name = name;
            Mark = mark;
            Description = description;
            Version = version;
            VersionMatches = versionMatches;
            Players = players;
            Capacity = capacity;
            Restriction = restriction;
        }

        internal ulong Id { get; }
        internal string Name { get; }
        internal string Mark { get; }
        internal string Description { get; }
        internal string Version { get; }
        internal bool VersionMatches { get; }
        internal int Players { get; }
        internal int Capacity { get; }
        internal string Restriction { get; }

        internal bool SameAs(Listing other)
        {
            return Id == other.Id && Name == other.Name && Mark == other.Mark
                && Description == other.Description && Version == other.Version
                && VersionMatches == other.VersionMatches && Players == other.Players
                && Capacity == other.Capacity && Restriction == other.Restriction;
        }
    }

    internal static class BrowserList
    {
        internal static IEnumerable<T> Select<T>(IEnumerable<T> entries, Func<T, Listing> getListing,
            string search, bool openToNewEmployees, SortOrder sort)
        {
            string[] terms = (search ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            var matches = entries.Where(entry =>
            {
                Listing listing = getListing(entry);
                return (!openToNewEmployees || listing.Restriction == null)
                    && terms.All(term => Contains(listing.Name, term) || Contains(listing.Mark, term)
                        || Contains(listing.Description, term));
            });

            var comparer = StringComparer.OrdinalIgnoreCase;
            if (sort == SortOrder.Players)
                return matches.OrderByDescending(entry => getListing(entry).Players)
                    .ThenBy(entry => getListing(entry).Name, comparer)
                    .ThenBy(entry => getListing(entry).Id);

            return matches.OrderBy(entry => getListing(entry).Name, comparer)
                .ThenBy(entry => getListing(entry).Id);
        }

        private static bool Contains(string value, string term)
        {
            return value != null && value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    internal sealed class RefreshSchedule
    {
        internal const int IntervalSeconds = 15;
        internal const int TimeoutSeconds = 25;
        private const int MaxFailureDelaySeconds = 120;
        private int _failures;

        internal float DueAt { get; private set; }

        internal void Succeeded(float now)
        {
            _failures = 0;
            DueAt = now + IntervalSeconds;
        }

        internal void Failed(float now)
        {
            _failures = Math.Min(_failures + 1, 3);
            DueAt = now + Math.Min(IntervalSeconds << _failures, MaxFailureDelaySeconds);
        }
    }
}
