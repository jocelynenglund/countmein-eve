using Alba;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

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
}
