using Pacman.Core.Enums;

namespace Pacman.Core.Entities;

public class Pacman : Entity
{
    public int Lives { get; set; } = 3;
    public int Score { get; set; } = 0;

    public Pacman()
    {
        Id = "pacman";
        Speed = 150f;
    }

    public void HandleInput(Direction input)
    {
        NextDirection = input;
    }

    public override void Update(float deltaTime)
    {
        if (IsAtTileCenter)
        {
            if (NextDirection != Direction.None && CanMoveInDirection(NextDirection))
            {
                CurrentDirection = NextDirection;
                NextDirection = Direction.None;
            }
            else if (CurrentDirection != Direction.None && !CanMoveInDirection(CurrentDirection))
            {
                CurrentDirection = Direction.None;
            }
            SnapToTileCenter();
        }

        base.Update(deltaTime);

        // Tunnel wrapping
        float maxX = MazeData.Width * MazeData.TileSize;
        if (X < -MazeData.TileSize) X = maxX;
        if (X > maxX) X = -MazeData.TileSize;
    }

    public string GetSprite()
    {
        return CurrentDirection switch
        {
            Direction.Up => "imgs/pacmanUp.png",
            Direction.Down => "imgs/pacmanDown.png",
            Direction.Left => "imgs/pacmanLeft.png",
            Direction.Right => "imgs/pacmanRight.png",
            _ => "imgs/pacmanRight.png"
        };
    }
}
