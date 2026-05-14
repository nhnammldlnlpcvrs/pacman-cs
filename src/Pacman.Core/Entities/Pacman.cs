using Pacman.Core.Enums;

namespace Pacman.Core.Entities;

public class Pacman : Entity
{
    public int Lives { get; set; } = 3;
    public int Score { get; set; } = 0;
    private readonly Queue<Direction> _inputBuffer = new();
    private const int MaxBufferSize = 3;

    public Pacman()
    {
        Id = "pacman";
        Speed = 150f;
    }

    public void HandleInput(Direction input)
    {
        if (input == Direction.None) return;

        Direction lastQueued = _inputBuffer.Count > 0
            ? _inputBuffer.Last()
            : CurrentDirection;

        if (input != lastQueued && _inputBuffer.Count < MaxBufferSize)
            _inputBuffer.Enqueue(input);
    }

    public override void Update(float deltaTime)
    {
        if (IsAtTileCenter)
        {
            while (_inputBuffer.Count > 0)
            {
                Direction next = _inputBuffer.Peek();
                if (CanMoveInDirection(next))
                {
                    CurrentDirection = next;
                    _inputBuffer.Dequeue();
                    break;
                }
                else
                {
                    if (next == CurrentDirection || next == CurrentDirection.Opposite())
                    {
                        _inputBuffer.Dequeue();
                        continue;
                    }
                    break;
                }
            }

            if (CurrentDirection != Direction.None && !CanMoveInDirection(CurrentDirection))
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
