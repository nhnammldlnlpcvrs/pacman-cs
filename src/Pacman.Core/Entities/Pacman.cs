using Pacman.Core.Enums;

namespace Pacman.Core.Entities;

public class Pacman : Entity
{
    public int Lives { get; set; } = 3;
    public int Score { get; set; } = 0;

    private readonly Queue<Direction> _inputBuffer = new();
    private const int MaxBufferSize = 3;
    private Direction _lastBuffered;

    public Pacman()
    {
        Id = "pacman";
        Speed = 150f;
    }

    public void HandleInput(Direction input)
    {
        if (input == Direction.None) return;

        // Determine what to compare against to avoid duplicates
        Direction compareTo = _inputBuffer.Count > 0
            ? _lastBuffered
            : CurrentDirection;

        if (input == compareTo) return;
        if (_inputBuffer.Count >= MaxBufferSize) return;

        _inputBuffer.Enqueue(input);
        _lastBuffered = input;
    }

    public override void Update(float deltaTime)
    {
        if (IsAtTileCenter)
        {
            // Consume buffered input — apply first valid direction
            int tries = _inputBuffer.Count;
            while (tries > 0)
            {
                tries--;
                Direction next = _inputBuffer.Peek();

                if (CanMoveInDirection(next))
                {
                    CurrentDirection = next;
                    _inputBuffer.Dequeue();
                    break;
                }

                // Discard stale directions (same as current or opposite)
                if (next == CurrentDirection || next == CurrentDirection.Opposite())
                {
                    _inputBuffer.Dequeue();
                    continue;
                }

                // Direction not valid yet — keep in buffer, stop trying
                break;
            }

            // Stop if current direction is blocked
            if (CurrentDirection != Direction.None && !CanMoveInDirection(CurrentDirection))
            {
                CurrentDirection = Direction.None;
            }

            SnapToTileCenter();
        }

        // Execute movement
        base.Update(deltaTime);

        // Tunnel wrapping — teleport from one side to the other
        float maxX = MazeData.Width * MazeData.TileSize;
        if (X < -MazeData.TileSize) X = maxX;
        else if (X > maxX) X = -MazeData.TileSize;
    }

    public void ClearInputBuffer()
    {
        _inputBuffer.Clear();
        _lastBuffered = Direction.None;
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
