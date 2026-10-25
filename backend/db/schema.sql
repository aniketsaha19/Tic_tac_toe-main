IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025015126_InitialCreate'
)
BEGIN
    CREATE TABLE [Games] (
        [Id] uniqueidentifier NOT NULL,
        [BoardState] nvarchar(9) NOT NULL,
        [CurrentPlayer] nvarchar(1) NOT NULL,
        [Mode] int NOT NULL,
        [Status] int NOT NULL,
        [Winner] nvarchar(1) NULL,
        [WinningCells] nvarchar(16) NULL,
        [ScoreApplied] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Games] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025015126_InitialCreate'
)
BEGIN
    CREATE TABLE [Scoreboards] (
        [Id] int NOT NULL IDENTITY,
        [XWins] int NOT NULL,
        [OWins] int NOT NULL,
        [Draws] int NOT NULL,
        CONSTRAINT [PK_Scoreboards] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025015126_InitialCreate'
)
BEGIN
    CREATE TABLE [Moves] (
        [Id] int NOT NULL IDENTITY,
        [GameId] uniqueidentifier NOT NULL,
        [MoveNumber] int NOT NULL,
        [Player] nvarchar(1) NOT NULL,
        [CellIndex] int NOT NULL,
        [IsComputerMove] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Moves] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Moves_Games_GameId] FOREIGN KEY ([GameId]) REFERENCES [Games] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025015126_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Draws', N'OWins', N'XWins') AND [object_id] = OBJECT_ID(N'[Scoreboards]'))
        SET IDENTITY_INSERT [Scoreboards] ON;
    EXEC(N'INSERT INTO [Scoreboards] ([Id], [Draws], [OWins], [XWins])
    VALUES (1, 0, 0, 0)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Draws', N'OWins', N'XWins') AND [object_id] = OBJECT_ID(N'[Scoreboards]'))
        SET IDENTITY_INSERT [Scoreboards] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025015126_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Moves_GameId_MoveNumber] ON [Moves] ([GameId], [MoveNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025015126_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261025015126_InitialCreate', N'10.0.13');
END;

COMMIT;
GO

