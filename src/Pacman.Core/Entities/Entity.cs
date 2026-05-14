using Pacman.Core.Enums;

namespace Pacman.Core.Entities;

public abstract class Entity
{
    public string Id { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public float Speed { get; set; } = 150f;
    public Direction CurrentDirection { get; set; } = Direction.None;
    public Direction NextDirection { get; set; } = Direction.None;
    public bool Visible { get; set; } = true;

    public int GridX => (int)Math.Round(X / MazeData.TileSize);
    public int GridY => (int)Math.Round(Y / MazeData.TileSize);

    public bool IsAtTileCenter
    {
        get
        {
            float cx = GridX * MazeData.TileSize;
            float cy = GridY * MazeData.TileSize;
            return Math.Abs(X - cx) < 2f && Math.Abs(Y - cy) < 2f;
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

        var (dx, dy) = CurrentDirection.Delta();
        float newX = X + dx * Speed * deltaTime;
        float newY = Y + dy * Speed * deltaTime;

        int targetGx = (int)Math.Round(newX / MazeData.TileSize);
        int targetGy = (int)Math.Round(newY / MazeData.TileSize);

        if (IsCellWalkable(targetGx, targetGy))
        {
            X = newX;
            Y = newY;
        }
        else
        {
            SnapToTileCenter();
        }
    }

    protected virtual bool IsCellWalkable(int gridX, int gridY)
        => MazeData.IsWalkable(gridX, gridY, false);

    public virtual bool CanMoveInDirection(Direction dir)
    {
        var (dx, dy) = dir.Delta();
        return MazeData.IsWalkable(GridX + dx, GridY + dy, false);
    }

    public void SetPosition(int gridX, int gridY)
    {
        X = gridX * MazeData.TileSize;
        Y = gridY * MazeData.TileSize;
    }
}
