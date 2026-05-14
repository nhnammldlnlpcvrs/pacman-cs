using Pacman.Core.Enums;

namespace Pacman.Core.Entities;

public enum GhostMode
{
    Chase,
    Scatter,
    Frightened,
    Eyes
}

public class Ghost : Entity
{
    public GhostMode Mode { get; set; } = GhostMode.Scatter;
    public string Color { get; set; } = "red";
    public int ScatterTargetX { get; set; }
    public int ScatterTargetY { get; set; }
    public int TargetGridX { get; set; }
    public int TargetGridY { get; set; }

    public Ghost(string id, string color, int scatterX, int scatterY)
    {
        Id = id;
        Color = color;
        ScatterTargetX = scatterX;
        ScatterTargetY = scatterY;
        Speed = 130f;
    }

    public override void Update(float deltaTime)
    {
        if (IsAtTileCenter)
        {
            ChooseDirection();
            SnapToTileCenter();
        }

        Speed = Mode switch
        {
            GhostMode.Frightened => 80f,
            GhostMode.Eyes => 200f,
            _ => 130f
        };

        base.Update(deltaTime);

        float maxX = MazeData.Width * MazeData.TileSize;
        if (X < -MazeData.TileSize) X = maxX;
        if (X > maxX) X = -MazeData.TileSize;
    }

    private void ChooseDirection()
    {
        int targetX, targetY;

        if (Mode == GhostMode.Frightened)
        {
            var dirs = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
            var valid = dirs.Where(d => d != CurrentDirection.Opposite() && CanMoveInDirection(d)).ToArray();
            if (valid.Length > 0)
                CurrentDirection = valid[Random.Shared.Next(valid.Length)];
            return;
        }

        targetX = Mode == GhostMode.Eyes ? 14 :
                  Mode == GhostMode.Scatter ? ScatterTargetX :
                  TargetGridX;
        targetY = Mode == GhostMode.Eyes ? 14 :
                  Mode == GhostMode.Scatter ? ScatterTargetY :
                  TargetGridY;

        var directions = new[] { Direction.Up, Direction.Left, Direction.Down, Direction.Right };
        Direction bestDir = CurrentDirection;
        double bestDist = double.MaxValue;

        foreach (var dir in directions)
        {
            if (dir == CurrentDirection.Opposite()) continue;
            if (!CanMoveInDirection(dir)) continue;

            var (dx, dy) = dir.Delta();
            int nx = GridX + dx;
            int ny = GridY + dy;
            double dist = (nx - targetX) * (nx - targetX) + (ny - targetY) * (ny - targetY);

            if (dist < bestDist)
            {
                bestDist = dist;
                bestDir = dir;
            }
        }

        CurrentDirection = bestDir;
    }

    protected override bool IsCellWalkable(int gridX, int gridY)
        => MazeData.IsWalkable(gridX, gridY, true);

    public override bool CanMoveInDirection(Direction dir)
    {
        var (dx, dy) = dir.Delta();
        return MazeData.IsWalkable(GridX + dx, GridY + dy, true);
    }

    public string GetSprite()
    {
        if (Mode == GhostMode.Frightened)
            return "imgs/scaredGhost.png";
        return $"imgs/{Color}Ghost.png";
    }
}
