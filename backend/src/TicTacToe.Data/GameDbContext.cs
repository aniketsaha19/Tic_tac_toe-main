using Microsoft.EntityFrameworkCore;
using TicTacToe.Data.Entities;

namespace TicTacToe.Data;

public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();

    public DbSet<Move> Moves => Set<Move>();

    public DbSet<Scoreboard> Scoreboards => Set<Scoreboard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>(game =>
        {
            game.Property(g => g.CurrentPlayer).HasMaxLength(1);
            game.Property(g => g.Winner).HasMaxLength(1);
            game.Property(g => g.Mode).HasConversion<int>();
            game.Property(g => g.Status).HasConversion<int>();
            game.HasMany(g => g.Moves)
                .WithOne(m => m.Game!)
                .HasForeignKey(m => m.GameId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Move>(move =>
        {
            move.Property(m => m.Player).HasMaxLength(1);
            move.HasIndex(m => new { m.GameId, m.MoveNumber }).IsUnique();
        });

        // The scoreboard is a singleton row; seeding it here means EnsureCreated brings it along.
        modelBuilder.Entity<Scoreboard>()
            .HasData(new Scoreboard { Id = Scoreboard.SingletonId });
    }
}
