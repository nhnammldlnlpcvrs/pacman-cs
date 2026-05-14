using Pacman.Core.Entities;
using Pacman.Core.Enums;
using PacmanPlayer = Pacman.Core.Entities.Pacman;

namespace Pacman.Core;

public enum GameState
{
    Start,
    Playing,
    Frightened,
    Dying,
    GameOver,
    Win
}

public class EntityState
{
    public string Id { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public string Sprite { get; set; } = "";
    public bool Visible { get; set; } = true;
    public bool IsMouthOpen { get; set; }
    public bool IsGhost { get; set; }
}

public class GameEngine
{
    private readonly int[,] _mazeCopy;
    public PacmanPlayer Pacman { get; }
    public List<Ghost> Ghosts { get; }
    public GameState State { get; private set; } = GameState.Start;
    public int PelletsRemaining { get; private set; }
    public int GhostCombo { get; set; }

    private double _frightenedTimer;
    private const double FrightenedDuration = 7.0;
    private double _mouthTimer;
    private bool _isMouthOpen;
    private readonly List<string> _soundEvents = new();

    public List<string> FlushSoundEvents()
    {
        var copy = new List<string>(_soundEvents);
        _soundEvents.Clear();
        return copy;
    }

    private void EmitSound(string name) => _soundEvents.Add(name);

    public GameEngine()
    {
        _mazeCopy = new int[MazeData.Height, MazeData.Width];
        Array.Copy(MazeData.Grid, _mazeCopy, MazeData.Grid.Length);

        Pacman = new PacmanPlayer();
        Pacman.SetPosition(14, 23);

        Ghosts = new List<Ghost>
        {
            new("ghost-red", "red", 25, 0),
            new("ghost-pink", "pink", 2, 0),
            new("ghost-blue", "blue", 25, 30),
            new("ghost-orange", "orange", 2, 30)
        };
        Ghosts[0].SetPosition(12, 12);
        Ghosts[1].SetPosition(14, 12);
        Ghosts[2].SetPosition(11, 12);
        Ghosts[3].SetPosition(17, 12);

        CountPellets();
    }

    private void CountPellets()
    {
        PelletsRemaining = 0;
        for (int y = 0; y < MazeData.Height; y++)
            for (int x = 0; x < MazeData.Width; x++)
                if (_mazeCopy[y, x] == 0 || _mazeCopy[y, x] == 2)
                    PelletsRemaining++;
    }

    public void Start()
    {
        if (State == GameState.Start)
        {
            State = GameState.Playing;
            EmitSound("start");
        }
    }

    public void HandleInput(Direction dir)
    {
        if (State == GameState.Start)
            Start();
        Pacman.HandleInput(dir);
    }

    public List<EntityState> Update(double deltaTime)
    {
        if (State != GameState.Playing && State != GameState.Frightened)
            return GetEntityStates();

        if (State == GameState.Frightened)
        {
            _frightenedTimer -= deltaTime;
            if (_frightenedTimer <= 0)
            {
                State = GameState.Playing;
                GhostCombo = 0;
                foreach (var g in Ghosts)
                    if (g.Mode == GhostMode.Frightened)
                        g.Mode = GhostMode.Scatter;
            }
        }

        Pacman.Update((float)deltaTime);

        _mouthTimer -= deltaTime;
        if (_mouthTimer <= 0)
        {
            _isMouthOpen = Pacman.CurrentDirection != Direction.None && !_isMouthOpen;
            _mouthTimer = 0.12;
        }

        EatPellets();

        foreach (var ghost in Ghosts)
        {
            if (ghost.Mode != GhostMode.Frightened && ghost.Mode != GhostMode.Eyes)
                ghost.Mode = GhostMode.Chase;

            ghost.TargetGridX = Pacman.GridX;
            ghost.TargetGridY = Pacman.GridY;
            ghost.Update((float)deltaTime);
        }

        CheckGhostCollisions();

        if (PelletsRemaining <= 0)
            State = GameState.Win;

        return GetEntityStates();
    }

    private void EatPellets()
    {
        int gx = Pacman.GridX;
        int gy = Pacman.GridY;

        if (gx < 0 || gx >= MazeData.Width || gy < 0 || gy >= MazeData.Height)
            return;

        int cell = _mazeCopy[gy, gx];
        if (cell == 0)
        {
            _mazeCopy[gy, gx] = 3;
            PelletsRemaining--;
            Pacman.Score += 10;
            EmitSound("eat");
        }
        else if (cell == 2)
        {
            _mazeCopy[gy, gx] = 3;
            PelletsRemaining--;
            Pacman.Score += 50;
            EmitSound("powerup");
            ActivatePowerPellet();
        }
    }

    private void ActivatePowerPellet()
    {
        State = GameState.Frightened;
        _frightenedTimer = FrightenedDuration;
        GhostCombo = 0;
        foreach (var ghost in Ghosts)
        {
            if (ghost.Mode != GhostMode.Eyes)
            {
                ghost.Mode = GhostMode.Frightened;
                ghost.CurrentDirection = ghost.CurrentDirection.Opposite();
            }
        }
    }

    private void CheckGhostCollisions()
    {
        float hitDist = MazeData.TileSize * 0.7f;

        foreach (var ghost in Ghosts)
        {
            float dist = MathF.Sqrt(
                (Pacman.X - ghost.X) * (Pacman.X - ghost.X) +
                (Pacman.Y - ghost.Y) * (Pacman.Y - ghost.Y)
            );

            if (dist < hitDist)
            {
                if (ghost.Mode == GhostMode.Frightened)
                {
                    GhostCombo++;
                    int points = GhostCombo switch
                    {
                        1 => 200, 2 => 400, 3 => 800, _ => 1600
                    };
                    Pacman.Score += points;
                    ghost.Mode = GhostMode.Eyes;
                    EmitSound("ghostEat");
                }
                else if (ghost.Mode != GhostMode.Eyes)
                {
                    Pacman.Lives--;
                    EmitSound("death");
                    if (Pacman.Lives <= 0)
                        State = GameState.GameOver;
                    else
                        ResetPositions();
                    return;
                }
            }
        }

        foreach (var ghost in Ghosts)
        {
            if (ghost.Mode == GhostMode.Eyes && ghost.GridX == 14 && ghost.GridY == 14)
                ghost.Mode = GhostMode.Scatter;
        }
    }

    private void ResetPositions()
    {
        State = GameState.Playing;
        Pacman.SetPosition(14, 23);
        Pacman.CurrentDirection = Direction.None;
        Pacman.NextDirection = Direction.None;

        Ghosts[0].SetPosition(12, 12); Ghosts[0].Mode = GhostMode.Scatter;
        Ghosts[1].SetPosition(14, 12); Ghosts[1].Mode = GhostMode.Scatter;
        Ghosts[2].SetPosition(11, 12); Ghosts[2].Mode = GhostMode.Scatter;
        Ghosts[3].SetPosition(17, 12); Ghosts[3].Mode = GhostMode.Scatter;
        GhostCombo = 0;
    }

    public int[,] GetMaze() => _mazeCopy;

    public void Restart()
    {
        Array.Copy(MazeData.Grid, _mazeCopy, MazeData.Grid.Length);
        CountPellets();

        Pacman.SetPosition(14, 23);
        Pacman.CurrentDirection = Direction.None;
        Pacman.NextDirection = Direction.None;
        Pacman.Score = 0;
        Pacman.Lives = 3;

        Ghosts[0].SetPosition(12, 12); Ghosts[0].Mode = GhostMode.Scatter;
        Ghosts[1].SetPosition(14, 12); Ghosts[1].Mode = GhostMode.Scatter;
        Ghosts[2].SetPosition(11, 12); Ghosts[2].Mode = GhostMode.Scatter;
        Ghosts[3].SetPosition(17, 12); Ghosts[3].Mode = GhostMode.Scatter;

        GhostCombo = 0;
        _frightenedTimer = 0;
        _mouthTimer = 0;
        _isMouthOpen = false;
        State = GameState.Start;
    }

    private List<EntityState> GetEntityStates()
    {
        var states = new List<EntityState>
        {
            new() { Id = Pacman.Id, X = Pacman.X, Y = Pacman.Y, Sprite = Pacman.GetSprite(), Visible = true, IsMouthOpen = _isMouthOpen, IsGhost = false }
        };

        foreach (var ghost in Ghosts)
        {
            states.Add(new EntityState
            {
                Id = ghost.Id, X = ghost.X, Y = ghost.Y,
                Sprite = ghost.GetSprite(), Visible = true, IsGhost = true
            });
        }

        return states;
    }
}
