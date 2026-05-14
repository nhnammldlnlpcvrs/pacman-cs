using Pacman.Core.Enums;

namespace Pacman.Core;

public static class Pathfinding
{
    public static Direction FindBestDirection(
        int startX, int startY,
        int targetX, int targetY,
        Direction currentDir,
        bool isGhost)
    {
        if (currentDir == Direction.None)
            return Direction.None;

        var directions = new[] { Direction.Up, Direction.Left, Direction.Down, Direction.Right };
        Direction opposite = currentDir.Opposite();

        Direction bestDir = currentDir;
        int bestSteps = int.MaxValue;

        foreach (var dir in directions)
        {
            if (dir == opposite) continue;

            var (dx, dy) = dir.Delta();
            int nx = startX + dx;
            int ny = startY + dy;

            if (!MazeData.IsWalkable(nx, ny, isGhost)) continue;

            int steps = BfsDistance(nx, ny, targetX, targetY, isGhost);
            if (steps < bestSteps)
            {
                bestSteps = steps;
                bestDir = dir;
            }
        }

        return bestDir;
    }

    private static int BfsDistance(int fromX, int fromY, int toX, int toY, bool isGhost)
    {
        if (fromX == toX && fromY == toY) return 0;

        bool[,] visited = new bool[MazeData.Height, MazeData.Width];
        var queue = new Queue<(int x, int y, int dist)>();
        queue.Enqueue((fromX, fromY, 0));
        visited[fromY, fromX] = true;

        // Up, Left, Down, Right
        var dirs = new (int dx, int dy)[] { (0, -1), (-1, 0), (0, 1), (1, 0) };

        while (queue.Count > 0)
        {
            var (x, y, dist) = queue.Dequeue();

            foreach (var (dx, dy) in dirs)
            {
                int nx = x + dx;
                int ny = y + dy;

                if (nx == toX && ny == toY) return dist + 1;

                if (nx < 0 || nx >= MazeData.Width || ny < 0 || ny >= MazeData.Height)
                    continue;
                if (visited[ny, nx]) continue;
                if (!MazeData.IsWalkable(nx, ny, isGhost)) continue;

                visited[ny, nx] = true;
                queue.Enqueue((nx, ny, dist + 1));
            }
        }

        return int.MaxValue;
    }
}
