# countmein-eve

"Count me in" rebuilt event-sourced on the Critter Stack (Marten + Wolverine).
It is one half of a comparison: the sibling `countmein-crapp` builds the same
six features with EF Core, DDD and repositories.

Behaviour comes from `/data/projects/count-me-in` (its plan and Given/When/Then scenarios).

## Features

1. Start Interest Check
2. Pitch view
3. Count Me In
4. Change My Mind
5. How It's Going view (owner only)
6. Close Interest Check

## Stack

- .NET 10, Marten 8 / WolverineFx 6 (`WolverineFx.Http`, `WolverineFx.Marten`)
- Wolverine aggregate workflow: static `Handle(command, [WriteAggregate] aggregate)` returns an event
- Marten inline snapshots for the aggregate and the two views
- xUnit, Shouldly, Alba (integration tests against real Postgres)

## Running the tests

Needs Postgres on `localhost:5436` (database `countmein_eve`, password `countmein`):

    docker run -d --name countmein-eve-db -e POSTGRES_PASSWORD=countmein -p 5436:5432 postgres:17
    docker exec countmein-eve-db psql -U postgres -c 'create database countmein_eve'
    dotnet test

The pure Given/When/Then tests need no database; the Alba tests do.
Override the connection string with `ConnectionStrings__Postgres`.

## Layout

- `src/CountMeIn/Events.cs`, `InterestCheck.cs`: events and the aggregate
- `src/CountMeIn/Features/`: one file per slice (command, Handle/endpoint)
- `tests/CountMeIn.Tests/`: scenarios per slice, plus `ApiTests` (Alba)
