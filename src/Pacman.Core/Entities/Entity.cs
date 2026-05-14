using Pacman.Core.Enums;

namespace Pacman.Core.Entities;

public abstract class Entity
{
    public string Id { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public float Speed { get; set; } = 150f;
    public Direction CurrentDirection { get; set; } = Direction.None;
    public bool Visible { get; set; } = true;

    public int GridX => (int)Math.Round(X / MazeData.TileSize, MidpointRounding.AwayFromZero);
    public int GridY => (int)Math.Round(Y / MazeData.TileSize, MidpointRounding.AwayFromZero);

    public bool IsAtTileCenter
    {
        get
        {
            float cx = GridX * MazeData.TileSize;
            float cy = GridY * MazeData.TileSize;
            return Math.Abs(X - cx) < 1.5f && Math.Abs(Y - cy) < 1.5f;
        }
    }

    public void SnapToTileCenter()
    {
        X = GridX * MazeData.TileSize;
        Y = GridY * MazeData.TileSize;
    }

    public virtual void Update(float deltaTime)
    {
        if (CurrentDirection == Direction.None) return;
        if (deltaTime <= 0f) return;

        var (dx, dy) = CurrentDirection.Delta();
        float step = Speed * deltaTime;

        // Cap step size to prevent tunneling through walls
        if (step > MazeData.TileSize * 0.9f)
            step = MazeData.TileSize * 0.9f;

        float newX = X + dx * step;
        float newY = Y + dy * step;

        int targetGx = (int)Math.Round(newX / MazeData.TileSize, MidpointRounding.AwayFromZero);
        int targetGy = (int)Math.Round(newY / MazeData.TileSize, MidpointRounding.AwayFromZero);

        // Only move if the target cell is walkable
        if (IsCellWalkable(targetGx, targetGy))
        {
            X = newX;
            Y = newY;
        }
        else
        {
            // Hit a wall — snap to current tile center
            SnapToTileCenter();
        }
    }

    protected virtual bool IsCellWalkable(int gridX, int gridY)
        => MazeData.IsWalkable(gridX, gridY, false);

    public virtual bool CanMoveInDirection(Direction dir)
    {
        if (dir == Direction.None) return false;
        var (dx, dy) = dir.Delta();
        return MazeData.IsWalkable(GridX + dx, GridY + dy, false);
    }

    public void SetPosition(int gridX, int gridY)
    {
        X = gridX * MazeData.TileSize;
        Y = gridY * MazeData.TileSize;
    }
}
