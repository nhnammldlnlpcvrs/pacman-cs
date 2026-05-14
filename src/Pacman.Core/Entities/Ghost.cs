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

    private GameEngine? _engine;

    public Ghost(string id, string color, int scatterX, int scatterY)
    {
        Id = id;
        Color = color;
        ScatterTargetX = scatterX;
        ScatterTargetY = scatterY;
        Speed = 130f;
    }

    public void SetEngine(GameEngine engine) => _engine = engine;

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
        if (Mode == GhostMode.Frightened)
        {
            var dirs = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
            var valid = dirs.Where(d => d != CurrentDirection.Opposite() && CanMoveInDirection(d)).ToArray();
            if (valid.Length > 0)
                CurrentDirection = valid[Random.Shared.Next(valid.Length)];
            return;
        }

        int targetX = Mode switch
        {
            GhostMode.Eyes => 14,
            GhostMode.Scatter => ScatterTargetX,
            _ => GetChaseTargetX()
        };

        int targetY = Mode switch
        {
            GhostMode.Eyes => 14,
            GhostMode.Scatter => ScatterTargetY,
            _ => GetChaseTargetY()
        };

        Direction bestDir = Pathfinding.FindBestDirection(
            GridX, GridY, targetX, targetY, CurrentDirection, true);

        CurrentDirection = bestDir;
    }

    // ── Chase personalities ──────────────────────────────

    private int GetChaseTargetX() => Color switch
    {
        "red" => _engine?.Pacman.GridX ?? TargetGridX,
        "pink" => GetPinkyTargetX(),
        "blue" => GetInkyTargetX(),
        "orange" => GetClydeTargetX(),
        _ => _engine?.Pacman.GridX ?? TargetGridX
    };

    private int GetChaseTargetY() => Color switch
    {
        "red" => _engine?.Pacman.GridY ?? TargetGridY,
        "pink" => GetPinkyTargetY(),
        "blue" => GetInkyTargetY(),
        "orange" => GetClydeTargetY(),
        _ => _engine?.Pacman.GridY ?? TargetGridY
    };

    // Pinky: targets 4 tiles ahead of Pacman (clamped to maze)
    private int GetPinkyTargetX()
    {
        var (dx, _) = _engine!.Pacman.CurrentDirection.Delta();
        return ClampX(_engine.Pacman.GridX + dx * 4);
    }

    private int GetPinkyTargetY()
    {
        var (_, dy) = _engine!.Pacman.CurrentDirection.Delta();
        return ClampY(_engine.Pacman.GridY + dy * 4);
    }

    // Inky: 2 * (pacman + 2 ahead) - Blinky position (clamped to maze)
    private int GetInkyTargetX()
    {
        var p = _engine!.Pacman;
        var blinky = _engine.Ghosts[0];
        var (dx, _) = p.CurrentDirection.Delta();
        int pivotX = p.GridX + dx * 2;
        return ClampX(pivotX + (pivotX - blinky.GridX));
    }

    private int GetInkyTargetY()
    {
        var p = _engine!.Pacman;
        var blinky = _engine.Ghosts[0];
        var (_, dy) = p.CurrentDirection.Delta();
        int pivotY = p.GridY + dy * 2;
        return ClampY(pivotY + (pivotY - blinky.GridY));
    }

    // Clyde: chase if distance > 8 tiles, else scatter
    private int GetClydeTargetX()
    {
        var p = _engine!.Pacman;
        double dist = Math.Sqrt(
            (GridX - p.GridX) * (GridX - p.GridX) +
            (GridY - p.GridY) * (GridY - p.GridY));
        return dist > 8 ? p.GridX : ScatterTargetX;
    }

    private int GetClydeTargetY()
    {
        var p = _engine!.Pacman;
        double dist = Math.Sqrt(
            (GridX - p.GridX) * (GridX - p.GridX) +
            (GridY - p.GridY) * (GridY - p.GridY));
        return dist > 8 ? p.GridY : ScatterTargetY;
    }

    private static int ClampX(int x) => Math.Clamp(x, 0, MazeData.Width - 1);
    private static int ClampY(int y) => Math.Clamp(y, 0, MazeData.Height - 1);

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
