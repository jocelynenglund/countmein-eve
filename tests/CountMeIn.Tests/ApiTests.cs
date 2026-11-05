using Alba;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Microsoft.Extensions.Hosting;
using CountMeIn.Features;

namespace CountMeIn.Tests;

/// <summary>End to end against real Postgres (localhost:5436 by default, see README).</summary>
public class ApiTests : IAsyncLifetime
{
    IAlbaHost _host = null!;

    public async Task InitializeAsync()
    {
        _host = await AlbaHost.For<Program>();
        await _host.Services.GetRequiredService<IDocumentStore>().Advanced.Clean.DeleteAllEventDataAsync();
        await _host.Services.GetRequiredService<IDocumentStore>().Advanced.Clean.DeleteAllDocumentsAsync();
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    Task Start(string id, int goal = 2, string key = "secret") => _host.Scenario(s =>
    {
        s.Post.Json(new { checkId = id, title = "Board games", pitch = "Monthly", goal, ownerKey = key, on = "2026-11-04" }).ToUrl("/checks");
        s.StatusCodeShouldBe(201);
    });

    Task CountIn(string id, string person, string name, int status = 204) => _host.Scenario(s =>
    {
        s.Post.Json(new { checkId = id, personId = person, name, on = "2026-11-04" }).ToUrl($"/checks/{id}/interest");
        s.StatusCodeShouldBe(status);
    });

    [Fact]
    public async Task Start_count_in_and_read_the_pitch()
    {
        await Start("c1");
        await CountIn("c1", "p1", "Ana");
        await CountIn("c1", "p2", "Ben");

        var pitch = (await _host.Scenario(s => s.Get.Url("/checks/c1/pitch"))).ReadAsText();
        pitch.ShouldContain("\"count\":2");
        pitch.ShouldContain("\"goalReached\":true");
        pitch.ShouldNotContain("Ana");
        pitch.ShouldNotContain("secret");
    }

    [Fact]
    public async Task Refusals_come_back_as_http_errors_with_the_reason()
    {
        await Start("c2");
        await CountIn("c2", "p1", "Ana");
        await CountIn("c2", "p1", "Ana", status: 409);
        await CountIn("c2", "p2", "", status: 400);
        await CountIn("nope", "p1", "Ana", status: 404);
    }

    [Fact]
    public async Task Change_my_mind_then_owner_sees_the_tally_and_closes()
    {
        await Start("c3");
        await CountIn("c3", "p1", "Ana");
        await _host.Scenario(s =>
        {
            s.Post.Json(new { checkId = "c3", personId = "p1", on = "2026-11-05" }).ToUrl("/checks/c3/withdraw");
            s.StatusCodeShouldBe(204);
        });

        await _host.Scenario(s => { s.Get.Url("/checks/c3/how-its-going"); s.StatusCodeShouldBe(403); });
        var owner = (await _host.Scenario(s =>
        {
            s.WithRequestHeader("X-Owner-Key", "secret");
            s.Get.Url("/checks/c3/how-its-going");
        })).ReadAsText();
        owner.ShouldContain("\"count\":0");
        owner.ShouldContain("2026-11-05");

        await _host.Scenario(s =>
        {
            s.Post.Json(new { checkId = "c3", ownerKey = "wrong", on = "2026-11-06" }).ToUrl("/checks/c3/close");
            s.StatusCodeShouldBe(403);
        });
        await _host.Scenario(s =>
        {
            s.Post.Json(new { checkId = "c3", ownerKey = "secret", on = "2026-11-06" }).ToUrl("/checks/c3/close");
            s.StatusCodeShouldBe(204);
        });
        (await _host.Scenario(s => s.Get.Url("/checks/c3/pitch"))).ReadAsText().ShouldContain("\"open\":false");
        await CountIn("c3", "p9", "Late", status: 409);
    }

    [Fact]
    public async Task Door_night_check_in_waitlist_and_turn_away()
    {
        await _host.Scenario(s =>
        {
            s.Post.Json(new
            {
                checkId = "c4", title = "Board games", goal = 1, ownerKey = "secret", on = "2026-11-04", capacity = 1,
                doorsOpen = "2026-11-14T18:00:00Z", doorsClose = "2026-11-14T20:00:00Z"
            }).ToUrl("/checks");
            s.StatusCodeShouldBe(201);
        });
        await CountIn("c4", "p1", "Ana");
        await CountIn("c4", "p2", "Ben");

        Task CheckIn(string person, string at, int status) => _host.Scenario(s =>
        {
            s.Post.Json(new { checkId = "c4", personId = person, at }).ToUrl("/checks/c4/check-in");
            s.StatusCodeShouldBe(status);
        });

        await CheckIn("p1", "2026-11-14T17:00:00Z", 409);   // doors not open yet
        await CheckIn("p1", "2026-11-14T18:30:00Z", 204);
        await CheckIn("p2", "2026-11-14T18:40:00Z", 409);   // waitlisted: refused, but remembered
        await CheckIn("p9", "2026-11-14T18:40:00Z", 409);   // not on the list

        var pitch = (await _host.Scenario(s => s.Get.Url("/checks/c4/pitch"))).ReadAsText();
        pitch.ShouldContain("\"spotsLeft\":0");
        pitch.ShouldContain("\"waitlistCount\":1");

        await _host.Scenario(s => { s.Get.Url("/checks/c4/door-list"); s.StatusCodeShouldBe(403); });
        var door = (await _host.Scenario(s =>
        {
            s.WithRequestHeader("X-Owner-Key", "secret");
            s.Get.Url("/checks/c4/door-list");
        })).ReadAsText();
        door.ShouldContain("\"checkedIn\":[{\"name\":\"Ana\"");
        door.ShouldContain("\"waitlist\":[\"Ben\"]");
        door.ShouldContain("\"turnedAway\":[{\"name\":\"Ben\"");
    }

    static string Iso(DateTimeOffset t) => t.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");

    async Task<string> DoorList(string id) => (await _host.Scenario(s =>
    {
        s.WithRequestHeader("X-Owner-Key", "secret");
        s.Get.Url($"/checks/{id}/door-list");
    })).ReadAsText();

    async Task<string> WaitForRelease(string id)
    {
        for (var i = 0; i < 40; i++)
        {
            var door = await DoorList(id);
            if (!door.Contains("\"letInFromWaitlist\":[]")) return door;
            await Task.Delay(500);
        }
        throw new TimeoutException("The release never happened");
    }

    [Fact]
    public async Task The_release_happens_by_itself_when_the_last_ten_minutes_begin()
    {
        var now = DateTimeOffset.UtcNow;
        await _host.Scenario(s =>
        {
            s.Post.Json(new
            {
                checkId = "c5", title = "Board games", goal = 1, ownerKey = "secret", on = "2026-11-04", capacity = 1,
                doorsOpen = Iso(now.AddHours(-1)), doorsClose = Iso(now.AddMinutes(10).AddSeconds(3))   // release in ~3 seconds
            }).ToUrl("/checks");
            s.StatusCodeShouldBe(201);
        });
        await CountIn("c5", "p1", "Ana");
        await CountIn("c5", "p2", "Ben");
        await _host.Scenario(s =>
        {
            s.Post.Json(new { checkId = "c5", personId = "p2", at = Iso(now) }).ToUrl("/checks/c5/check-in");
            s.StatusCodeShouldBe(409);   // waitlisted, remembered
        });

        var door = await WaitForRelease("c5");
        door.ShouldContain("\"letInFromWaitlist\":[\"Ben\"]");
        door.ShouldContain("\"lostTheirSpot\":[\"Ana\"]");

        await _host.Scenario(s =>
        {
            s.Post.Json(new { checkId = "c5", personId = "p1", at = Iso(DateTimeOffset.UtcNow) }).ToUrl("/checks/c5/check-in");
            s.StatusCodeShouldBe(409);   // Your spot went to the waitlist
        });
        await _host.Scenario(s =>
        {
            s.Post.Json(new { checkId = "c5", personId = "p2", at = Iso(DateTimeOffset.UtcNow) }).ToUrl("/checks/c5/check-in");
            s.StatusCodeShouldBe(204);
        });
    }

    [Fact]
    public async Task A_check_from_before_this_shipped_is_picked_up_on_startup()
    {
        // A check as the previous version left it: events appended, no scheduled message, and a stored
        // snapshot that knows nothing about who tried at the door.
        var now = DateTimeOffset.UtcNow;
        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using (var session = store.LightweightSession())
        {
            session.Events.StartStream<InterestCheck>("c6",
                new InterestCheckStarted("c6", "Board games", "", 1, null, OwnerKey.Hash("secret"), 1,
                    now.AddHours(-1), now.AddMinutes(10).AddSeconds(3)),
                new InterestShown("c6", "p1", "Ana", null, new DateOnly(2026, 11, 4)),
                new InterestShown("c6", "p2", "Ben", null, new DateOnly(2026, 11, 4)),
                new TurnedAwayAtDoor("c6", "p2", now));
            await session.SaveChangesAsync();

            session.QueueSqlCommand(
                "update countmein.mt_doc_interestcheck set data = data - 'TriedAtDoor' - 'Released' - 'LetIn' - 'LostTheirSpot'");
            await session.SaveChangesAsync();
        }

        foreach (var service in _host.Services.GetServices<IHostedService>().OfType<ScheduleReleases>())
            await service.StartAsync(CancellationToken.None);

        var door = await WaitForRelease("c6");
        door.ShouldContain("\"letInFromWaitlist\":[\"Ben\"]");
    }

    [Fact]
    public async Task The_venue_is_revealed_only_to_those_who_hold_a_spot_when_the_time_comes()
    {
        await _host.Scenario(s => { s.Post.Json(new { checkId = "c7", title = "x", goal = 1, ownerKey = "secret", on = "2026-11-04", capacity = 1 }).ToUrl("/checks"); s.StatusCodeShouldBe(201); });
        await CountIn("c7", "p1", "Ana");
        await CountIn("c7", "p2", "Ben");   // waitlisted

        var revealAt = DateTimeOffset.UtcNow.AddSeconds(3);
        await _host.Scenario(s =>
        {
            s.Post.Json(new { checkId = "c7", ownerKey = "secret", venue = "The Library", revealAt = Iso(revealAt), at = Iso(DateTimeOffset.UtcNow) })
                .ToUrl("/checks/c7/venue");
            s.StatusCodeShouldBe(204);
        });

        Task<string> Ask(string person) => _host.Scenario(s => s.Get.Url($"/checks/c7/venue?personId={person}")).ContinueWith(t => t.Result.ReadAsText());
        (await Ask("p1")).ShouldNotContain("The Library");   // before the reveal nobody sees it
        await Task.Delay(4000);
        (await Ask("p1")).ShouldContain("The Library");
        (await Ask("p2")).ShouldNotContain("The Library");

        await _host.Scenario(s =>
        {
            s.Post.Json(new { checkId = "c7", ownerKey = "secret", venue = "Elsewhere", revealAt = Iso(revealAt), at = Iso(DateTimeOffset.UtcNow) })
                .ToUrl("/checks/c7/venue");
            s.StatusCodeShouldBe(409);
        });
        (await DoorList("c7")).ShouldContain("\"venue\":\"The Library\"");
    }
}
