using System;
using System.Collections.Generic;
using System.Linq;
using EnusDE.MultiplayerBrowser;

var entries = new[]
{
    new Listing(2, "Alarka", "AR", "Passenger jobs", "0.6.2", true, 3, 32, null),
    new Listing(1, "Alarka", "AA", "Freight only", "0.6.2", true, 2, 0, "Password"),
    new Listing(3, "Bryson", "BY", null, "0.6.2", true, 7, 32, null)
};

IEnumerable<ulong> Ids(string search, bool openOnly, SortOrder order) =>
    BrowserList.Select(entries, entry => entry, search, openOnly, order).Select(entry => entry.Id);

Check(Ids("ar passenger", false, SortOrder.Name).SequenceEqual(new ulong[] { 2 }),
    "Case-insensitive multi-term search across name, mark, and description");
Check(Ids("  ", false, SortOrder.Name).SequenceEqual(new ulong[] { 1, 2, 3 }),
    "Empty search and deterministic name tie-break");
Check(Ids("", false, SortOrder.Players).SequenceEqual(new ulong[] { 3, 2, 1 }),
    "Descending player count");
Check(Ids("alarka", true, SortOrder.Name).SequenceEqual(new ulong[] { 2 }),
    "New employee restrictions filter");
Check(!Ids("no results", false, SortOrder.Players).Any(),
    "No matching rows");
Check(entries[0].SameAs(new Listing(2, "Alarka", "AR", "Passenger jobs", "0.6.2", true, 3, 32, null)),
    "Equivalent snapshots can reuse rows");
Check(!entries[0].SameAs(new Listing(2, "Alarka", "AR", "Passenger jobs", "0.6.2", true, 4, 32, null)),
    "Changed player count invalidates the cached row");
Check(!entries[0].SameAs(new Listing(2, "Alarka", "AR", "Passenger jobs", "0.6.2", true, 3, 0, null)),
    "Changed capacity invalidates the cached row");

var schedule = new RefreshSchedule();
schedule.Succeeded(10);
Check(schedule.DueAt == 25, "15-second refresh interval");
schedule.Failed(25);
Check(schedule.DueAt == 55, "First failure waits 30 seconds");
schedule.Failed(55);
Check(schedule.DueAt == 115, "Second failure waits 60 seconds");
schedule.Failed(115);
schedule.Failed(235);
Check(schedule.DueAt == 355, "Further failures wait up to 120 seconds");
schedule.Succeeded(355);
schedule.Failed(370);
Check(schedule.DueAt == 400, "Success resets the failure backoff");

Console.WriteLine("All 13 offline checks passed.");

static void Check(bool passed, string name)
{
    if (!passed)
        throw new InvalidOperationException(name);
    Console.WriteLine("PASS " + name);
}
