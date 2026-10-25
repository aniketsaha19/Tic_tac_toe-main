# Tic Tac Toe — Angular + .NET + SQL Server

A browser Tic Tac Toe game. The Angular frontend is a thin renderer; the .NET Web API owns the
board, the turn, win/draw detection, move history, undo, the scoreboard and the computer opponent,
and persists all of it in SQL Server.

## Tech stack

| Layer | Choice |
| --- | --- |
| Frontend | Angular 20, TypeScript, standalone components, signals |
| Backend | .NET 10 Web API (controllers) |
| API style | REST / JSON |
| Storage | SQL Server 2022 via EF Core 10 |
| Tests | xUnit (backend), Karma + Jasmine (frontend) |

## Features

- 3x3 board, empty cells clickable, played cells locked
- Alternating X / O turns with the current player shown; invalid moves never change the turn
- Win detection on rows, columns and both diagonals, with the winning cells highlighted
- Draw detection on a full board
- Move history (move number, player, row/column) updated after every valid move
- Undo, mode-aware: one move in two-player mode, the computer move plus the human move in computer mode
- Session scoreboard (X wins / O wins / draws), updated once per completed game, with its own reset
- Reset Game (clears board, history and status; keeps the scoreboard) and Reset Scoreboard
- Two Player Mode and Play Against Computer (human is X, computer is O)
- Everything persisted in SQL Server, so state survives a backend restart

## Prerequisites

- .NET SDK 10.0 or later
- Node.js 22.12+ (developed on 22.17.1) and npm
- SQL Server reachable at `LWO-LAP1626` with SQL login `sa`

## Run the backend

```bash
cd backend/src/TicTacToe.Api
dotnet run
```

The API listens on `http://localhost:5012`. On startup it applies any pending EF Core migration
(`Database.Migrate()`), which on a fresh machine creates the `TicTacToe` database, its tables and
the seeded scoreboard row. Nothing else to run.

The connection string lives in `backend/src/TicTacToe.Api/appsettings.json`:

```
Server=LWO-LAP1626;Database=TicTacToe;User Id=sa;Password=password-1;TrustServerCertificate=True;Encrypt=False
```

Point it at another instance by editing that value or setting
`ConnectionStrings__GameDb` in the environment.

### Setting the database up on another PC

The app does it for you: set the connection string, run `dotnet run`, done.

If the database is created by hand or by a DBA instead, run the checked-in schema script. It is
idempotent — safe to run against a database that is already up to date — but it does not create the
database itself:

```bash
sqlcmd -S <server> -U sa -P <password> -Q "IF DB_ID('TicTacToe') IS NULL CREATE DATABASE TicTacToe"
sqlcmd -S <server> -U sa -P <password> -d TicTacToe -i backend/db/schema.sql
```

`backend/db/schema.sql` is generated from the EF Core migrations in
`backend/src/TicTacToe.Data/Migrations`, so it always matches the entity model. Regenerate it after
changing an entity:

```bash
cd backend
dotnet tool restore
dotnet dotnet-ef migrations add <Name> --project src/TicTacToe.Data --startup-project src/TicTacToe.Api
dotnet dotnet-ef migrations script --idempotent --project src/TicTacToe.Data --startup-project src/TicTacToe.Api --output db/schema.sql
```

`dotnet tool restore` installs the pinned `dotnet-ef` from `backend/dotnet-tools.json`, so no global
tool install is needed. Stop the running API first — it holds a lock on its own binary and the
build will fail otherwise.

A database that was created by an earlier build (before migrations existed) has the tables but no
`__EFMigrationsHistory`, which makes `Migrate()` fail on startup. Baseline it once:

```sql
CREATE TABLE [__EFMigrationsHistory](
  [MigrationId] nvarchar(150) NOT NULL,
  [ProductVersion] nvarchar(32) NOT NULL,
  CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId]));
INSERT INTO [__EFMigrationsHistory] VALUES (N'20261025015126_InitialCreate', N'10.0.13');
```

OpenAPI document (Development): `http://localhost:5012/openapi/v1.json`.
`backend/src/TicTacToe.Api/TicTacToe.Api.http` has a ready-made request for every endpoint.

## Run the frontend

```bash
cd frontend/tictactoe-web
npm install
npm start
```

Open `http://localhost:4200`. The API base URL is in
`frontend/tictactoe-web/src/environments/environment.ts`; the backend allows CORS from
`http://localhost:4200`.

## API

| Method | Endpoint | Purpose |
| --- | --- | --- |
| POST | `/api/games` | Create a game session. Body: `{ "mode": 0 }` (0 = TwoPlayer, 1 = Computer) |
| GET | `/api/games/{id}` | Current game state |
| POST | `/api/games/{id}/moves` | Submit a move. Body: `{ "player": "X", "row": 0, "column": 0 }` or `{ "player": "X", "cellIndex": 0 }` |
| POST | `/api/games/{id}/undo` | Undo, per the game's mode |
| POST | `/api/games/{id}/reset` | Clear board, history and status; scoreboard untouched |
| GET | `/api/scoreboard` | Read the scoreboard |
| POST | `/api/scoreboard/reset` | Zero the scoreboard |

Every game endpoint returns the same game-state payload:

```json
{
  "id": "2073454e-6be9-4268-90eb-9f535a3d3f5a",
  "board": ["X", "", "", "", "O", "", "", "", ""],
  "currentPlayer": "X",
  "mode": 0,
  "status": 0,
  "winner": null,
  "winningCells": [],
  "moves": [
    { "moveNumber": 1, "player": "X", "row": 1, "column": 1, "cellIndex": 0, "isComputerMove": false }
  ],
  "canUndo": true,
  "scoreboard": { "xWins": 1, "oWins": 0, "draws": 0 }
}
```

`mode`: 0 = TwoPlayer, 1 = Computer. `status`: 0 = InProgress, 1 = Won, 2 = Draw.
`board` is row-major with `""` for an empty cell; `row`/`column` in the move history are 1-based for
display, while `row`/`column` in a move request are 0-based.

Rejected moves return `400` with `{ "error": "..." }` — occupied cell, cell outside the board, wrong
player, or a move after the game is complete. An unknown game id returns `404`.

## Run the tests

```bash
cd backend
dotnet test
```

41 xUnit tests. `GameRulesTests` and `ComputerPlayerTests` cover the pure rules; `GameServiceTests`
drives the real service over a SQLite in-memory database, so `dotnet test` needs no SQL Server.
Covered: valid move, each invalid-move kind, turn switching, row/column/diagonal win, draw, reset,
undo in both modes, scoreboard update and update-only-once, scoreboard reset, computer move
selection by priority, and a move after completion.

```bash
cd frontend/tictactoe-web
npm test -- --watch=false --browsers=ChromeHeadless
```

Frontend specs cover board rendering plus the API calls the game service makes.

## Design decisions

- **Backend is the source of truth.** The Angular app holds no game rules: it posts an action and
  renders whatever state comes back. Every mutating endpoint returns the full game state, so the UI
  never derives anything the backend could contradict.
- **SQL Server instead of in-memory.** The exercise allows in-memory storage; persisting instead
  means game state survives a backend restart and the move history is a real transaction log.
- **`TicTacToe.Core` has no dependencies.** Win detection, validation and the computer opponent are
  pure functions over a 9-character board string, which keeps them trivially testable and keeps EF
  Core out of the rules.
- **EF Core migrations, applied on startup.** `Database.Migrate()` sets a new machine up with no
  extra step, and `backend/db/schema.sql` (generated from the same migrations, idempotent) covers
  the case where the database is created by hand instead.
- **`dotnet-ef` pinned as a local tool** in `backend/dotnet-tools.json`, so `dotnet tool restore`
  gives every machine the same version and nothing has to be installed globally.
- **Scoreboard guarded by `Game.ScoreApplied`.** A completed game can only contribute to the
  scoreboard once, whatever traffic arrives afterwards.
- **Computer reply in the same request.** A human move in computer mode returns the board with the
  computer's answer already in it — one round trip, and the two moves commit together.
- **Deterministic computer.** Priority is win, block, center, corner, any free cell, with fixed cell
  order inside each rung so its behaviour is reproducible in tests.

## Clarifications and assumptions

- **Scoreboard and Undo: Option A.** Once a game is won or drawn, Undo is disabled and the
  scoreboard entry is final. Reset Game starts a fresh board without touching the scoreboard.
- The scoreboard is a single global row (`Id = 1`) shared by every client, matching the exercise's
  single-session framing.
- Reset Game reuses the same game id rather than creating a new session, so a reviewer's open tab
  keeps working.
- In computer mode only X is accepted from the client; a request to play O is rejected.
- The SQL credentials sit in `appsettings.json` in plain text because this is a local review
  exercise on a developer machine.
- Move history row/column are shown 1-based ("Row 1, Column 1") as in the exercise document.

## Known limitations

- No authentication and no per-user separation: one shared scoreboard, and any client that knows a
  game id can play it.
- No concurrency control on a game row — two simultaneous moves on the same game could interleave.
- The computer opponent follows the fixed priority list from the specification, so it is beatable;
  it is not a minimax player.
- No pagination or cleanup of old games; the tables grow for as long as the database lives.

## Future improvements

- Optimistic concurrency (a row version on `Game`) to make simultaneous moves safe
- Per-player or per-session scoreboards once there are real users
- Minimax (or a difficulty setting) for the computer opponent
- A game list / resume screen, since every game is already persisted

## AI tools and prompt summary

Built with Claude (Claude Code) from the supplied requirements document.

**Workflow**

1. The requirements document was read end to end and converted into a written plan: project layout,
   entities, endpoint table, DTO shape, undo semantics per mode, and the test list — before any code.
2. Two requirement gaps were resolved explicitly rather than guessed: Undo after completion
   (Option A chosen) and scoreboard scope (one global row). Persistence in SQL Server was the
   additional requirement on top of the document.
3. Code was generated layer by layer, each layer verified before the next: Core rules → unit tests →
   EF Core data layer → service and controllers → service tests → Angular frontend.
4. The API was exercised end to end against the real SQL Server instance (create, move by row/column
   and by index, rejected moves, win, undo after completion, reset, computer reply, computer undo,
   404) and the resulting rows inspected with `sqlcmd`.

**Prompts, in substance**

- "Build this exercise: Angular frontend, .NET Web API backend, backend owns all state, persist in
  SQL Server `LWO-LAP1626`, separate backend and frontend folders with their own dependencies."
- "Which undo policy, which scoreboard scope, how much goes in the database?" — answered before
  implementation, and recorded above.
- Per layer: "pure rules with no framework dependency", "mode-aware undo", "scoreboard updates once
  per completed game", "every endpoint returns the full game state".

**Reviewed and adjusted by hand**

- Undo recalculation: the board, turn, status and move numbering are rebuilt by replaying the
  surviving moves rather than patched in place, so an undo cannot leave a stale status behind.
- The `ScoreApplied` flag was added after thinking through what happens when a finished game is
  re-read or receives more traffic.
- NuGet was repointed at `nuget.org` (`backend/NuGet.config`) because the machine's default feed is
  an internal HTTP source that modern NuGet refuses, and `System.Security.Cryptography.Pkcs` was
  pinned to a patched version to clear a transitive advisory.
- Angular CLI was pinned to 20, as the latest CLI requires a newer Node than this machine has.
