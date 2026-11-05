using CountMeIn;
using CountMeIn.Features;
using JasperFx.Events;
using JasperFx.Events.Projections;
using Marten;
using Wolverine;
using Wolverine.Http;
using Wolverine.Marten;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMarten(opts =>
    {
        opts.Connection(builder.Configuration.GetConnectionString("Postgres")!);
        opts.DatabaseSchemaName = "countmein";
        opts.Events.StreamIdentity = StreamIdentity.AsString;
        opts.Projections.Snapshot<InterestCheck>(SnapshotLifecycle.Inline);
        opts.Projections.Snapshot<Pitch>(SnapshotLifecycle.Inline);
        opts.Projections.Snapshot<HowItsGoing>(SnapshotLifecycle.Inline);
        opts.Projections.Snapshot<DoorList>(SnapshotLifecycle.Inline);
    })
    .UseLightweightSessions()
    .IntegrateWithWolverine();

builder.Host.UseWolverine();
builder.Services.AddWolverineHttp();

var app = builder.Build();

// A refusal is a normal answer, not a crash.
app.Use(async (http, next) =>
{
    try { await next(http); }
    catch (Refusal r) { http.Response.StatusCode = r.Status; await http.Response.WriteAsJsonAsync(new { error = r.Message }); }
});

app.MapWolverineEndpoints();
app.Run();

public partial class Program;
