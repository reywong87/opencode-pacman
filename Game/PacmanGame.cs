namespace open_pacman.Game;

public sealed class PacmanGame
{
    private const double PacmanSpeed = .125;
    private const double GhostSpeed = .1;
    private const int TunnelRow = 14;
    private static readonly Dictionary<string, (int X, int Y)> Directions = new()
    {
        ["left"] = (-1, 0), ["right"] = (1, 0), ["up"] = (0, -1), ["down"] = (0, 1)
    };
    private static readonly Dictionary<string, string> Opposite = new()
    {
        ["left"] = "right", ["right"] = "left", ["up"] = "down", ["down"] = "up"
    };
    private static readonly string[] GhostDecisionDirections = ["up", "left", "down", "right"];
    private static readonly Dictionary<string, (int X, int Y)> GhostCorners = new()
    {
        ["red"] = (27, 0), ["pink"] = (0, 0), ["cyan"] = (27, 30), ["orange"] = (0, 30)
    };
    private static readonly (int X, int Y) PacmanStart = (13, 23);
    private static readonly (int X, int Y, string Kind, int ReleaseScore)[] GhostStarts =
    [
        (13, 14, "red", 0),
        (14, 14, "pink", 100),
        (13, 15, "cyan", 300),
        (14, 15, "orange", 600)
    ];
    private static readonly (int X, int Y)[][] GhostExitPaths =
    [
        [(13, 13), (13, 12), (13, 11)],
        [(13, 14), (13, 13), (13, 12), (13, 11)],
        [(13, 14), (13, 13), (13, 12), (13, 11)],
        [(13, 15), (13, 14), (13, 13), (13, 12), (13, 11)]
    ];
    private static readonly (int X, int Y)[] PowerPelletPositions =
    [
        (1, 3), (26, 3), (1, 23), (26, 23)
    ];
    private static readonly string[] MazeRows =
    [
        "############################", "#............##............#", "#.####.#####.##.#####.####.#", "#.####.#####.##.#####.####.#", "#.####.#####.##.#####.####.#", "#..........................#", "#.####.##.########.##.####.#", "#.####.##.########.##.####.#", "#......##....##....##......#", "######.#####.##.#####.######", "######.#####.##.#####.######", "######.##..........##.######", "######.##.###--###.##.######", "######.##.#      #.##.######", "          #      #          ", "######.##.#      #.##.######", "######.##.########.##.######", "######.##..........##.######", "######.#####.##.#####.######", "######.#####.##.#####.######", "#............##............#", "#.####.#####.##.#####.####.#", "#.####.#####.##.#####.####.#", "#...##................##...#", "###.##.##.########.##.##.###", "###.##.##.########.##.##.###", "#......##....##....##......#", "#.##########.##.##########.#", "#.##########.##.##########.#", "#..........................#", "############################"
    ];

    private int[][] grid = [];
    private Actor pacman = null!;
    private List<Actor> ghosts = [];
    private int score, lives, dots, dotsEatenThisLife;
    public string State { get; private set; } = "start";

    public PacmanGame() => Reset("start");
    public GameFrame Frame => new(grid, score, lives, State, new(pacman.X, pacman.Y, pacman.Direction), ghosts.Select(g => new ActorFrame(g.X, g.Y, g.Direction)).ToArray());

    public void Reset() => Reset("playing");
    public void SetDirection(string direction) { if (State == "playing" && Directions.ContainsKey(direction)) pacman.NextDirection = direction; }

    public void Update()
    {
        if (State != "playing") return;
        MovePacman();
        ReleaseGhosts();
        foreach (var ghost in ghosts) MoveGhost(ghost);
        if (ghosts.Any(g => g.Released && !g.LeavingHouse && Math.Abs(g.X - pacman.X) < .5 && Math.Abs(g.Y - pacman.Y) < .5))
        {
            if (--lives == 0) { State = "lost"; return; }
            ResetPositions();
        }
        if (dots == 0) State = "won";
    }

    private void Reset(string state)
    {
        grid = MazeRows.Select(row => row.Select(c => c == '#' ? 1 : c == '.' ? 2 : c == '-' ? 3 : 0).ToArray()).ToArray();
        foreach (var pellet in PowerPelletPositions) grid[pellet.Y][pellet.X] = 4;
        grid[PacmanStart.Y][PacmanStart.X] = 0;
        dots = grid.Sum(row => row.Count(cell => cell == 2)); score = 0; lives = 3; dotsEatenThisLife = 0; State = state;
        pacman = new(PacmanStart.X, PacmanStart.Y, "left", PacmanSpeed, "pacman", []);
        ghosts = GhostStarts.Select((g, i) => new Actor(g.X, g.Y, "up", GhostSpeed, g.Kind, GhostExitPaths[i])
        {
            Released = g.ReleaseScore == 0,
            LeavingHouse = g.ReleaseScore == 0
        }).ToList();
    }

    private void MovePacman()
    {
        if (Aligned(pacman))
        {
            Snap(pacman);
            if (pacman.NextDirection is { } next && CanMove(pacman.X, pacman.Y, next, true)) { pacman.Direction = next; pacman.NextDirection = null; }
            if (grid[(int)pacman.Y][(int)pacman.X] == 2) { grid[(int)pacman.Y][(int)pacman.X] = 0; score += 10; dots--; dotsEatenThisLife += 10; }
            if (!CanMove(pacman.X, pacman.Y, pacman.Direction, true)) return;
        }
        Move(pacman);
    }

    private void MoveGhost(Actor ghost)
    {
        if (!ghost.Released) return;
        if (ghost.LeavingHouse)
        {
            MoveOutOfHouse(ghost);
            return;
        }
        if (Aligned(ghost)) { Snap(ghost); DecideGhost(ghost); if (!CanMove(ghost.X, ghost.Y, ghost.Direction, false)) return; }
        Move(ghost);
    }

    private void MoveOutOfHouse(Actor ghost)
    {
        if (!Aligned(ghost)) { Move(ghost); return; }

        Snap(ghost);
        if (ghost.ExitPathIndex == ghost.ExitPath.Length)
        {
            ghost.LeavingHouse = false;
            return;
        }

        var target = ghost.ExitPath[ghost.ExitPathIndex];
        if (ghost.X == target.X && ghost.Y == target.Y)
        {
            ghost.ExitPathIndex++;
            MoveOutOfHouse(ghost);
            return;
        }

        ghost.Direction = target.X < ghost.X ? "left" : target.X > ghost.X ? "right" : target.Y < ghost.Y ? "up" : "down";
        Move(ghost);
    }

    private void ReleaseGhosts()
    {
        for (var i = 0; i < ghosts.Count; i++)
        {
            if (dotsEatenThisLife < GhostStarts[i].ReleaseScore) break;
            if (ghosts[i].Released) continue;
            ghosts[i].Released = true;
            ghosts[i].LeavingHouse = true;
            ghosts[i].AiCycleStartedAt = DateTime.UtcNow;
        }
    }

    private void DecideGhost(Actor ghost)
    {
        var choices = GhostDecisionDirections.Where(d => d != Opposite[ghost.Direction] && CanMove(ghost.X, ghost.Y, d, false)).ToArray();
        if (choices.Length == 0) choices = [Opposite[ghost.Direction]];
        var target = NormalizeGhostTarget(GhostTarget(ghost));
        var direction = choices[0];
        var distance = int.MaxValue;

        foreach (var choice in choices)
        {
            var delta = Directions[choice];
            var x = (int)ghost.X + delta.X;
            var y = (int)ghost.Y + delta.Y;
            if (y == TunnelRow && x < 0) x = grid[0].Length - 1;
            else if (y == TunnelRow && x >= grid[0].Length) x = 0;

            var candidateDistance = ShortestGhostPathDistance((x, y), target);
            if (candidateDistance < distance)
            {
                direction = choice;
                distance = candidateDistance;
            }
        }

        ghost.Direction = direction;
    }

    private (double X, double Y) GhostTarget(Actor ghost) => ghost.Kind switch
    {
        "red" => (Math.Round(pacman.X), Math.Round(pacman.Y)),
        "pink" => AheadOfPacman(4),
        "cyan" when (DateTime.UtcNow - ghost.AiCycleStartedAt).TotalSeconds % 14 < 7 => (Math.Round(pacman.X), Math.Round(pacman.Y)),
        "cyan" => GhostCorners["cyan"],
        "orange" when Math.Abs(ghost.X - pacman.X) + Math.Abs(ghost.Y - pacman.Y) >= 8 => (Math.Round(pacman.X), Math.Round(pacman.Y)),
        "orange" => GhostCorners["orange"],
        _ => (ghost.X, ghost.Y)
    };

    private (double X, double Y) AheadOfPacman(int tiles)
    {
        var direction = Directions[pacman.Direction];
        return (Math.Round(pacman.X) + direction.X * tiles, Math.Round(pacman.Y) + direction.Y * tiles);
    }

    private (int X, int Y) NormalizeGhostTarget((double X, double Y) target)
    {
        var x = Math.Clamp((int)Math.Round(target.X), 0, grid[0].Length - 1);
        var y = Math.Clamp((int)Math.Round(target.Y), 0, grid.Length - 1);
        if (IsGhostTraversable(x, y)) return (x, y);

        return Enumerable.Range(0, grid.Length)
            .SelectMany(candidateY => Enumerable.Range(0, grid[0].Length).Select(candidateX => (X: candidateX, Y: candidateY)))
            .Where(tile => IsGhostTraversable(tile.X, tile.Y))
            .OrderBy(tile => Math.Abs(tile.X - x) + Math.Abs(tile.Y - y))
            .ThenBy(tile => tile.Y)
            .ThenBy(tile => tile.X)
            .First();
    }

    private IEnumerable<(int X, int Y)> GhostNeighbors((int X, int Y) tile)
    {
        foreach (var direction in GhostDecisionDirections)
        {
            var delta = Directions[direction];
            var x = tile.X + delta.X;
            var y = tile.Y + delta.Y;
            if (y == TunnelRow && x < 0) x = grid[0].Length - 1;
            else if (y == TunnelRow && x >= grid[0].Length) x = 0;
            if (IsGhostTraversable(x, y)) yield return (x, y);
        }
    }

    private int ShortestGhostPathDistance((int X, int Y) start, (int X, int Y) target)
    {
        var queue = new Queue<((int X, int Y) Tile, int Distance)>();
        var visited = new HashSet<(int X, int Y)> { start };
        queue.Enqueue((start, 0));

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.Tile == target) return current.Distance;

            foreach (var neighbor in GhostNeighbors(current.Tile))
            {
                if (visited.Add(neighbor)) queue.Enqueue((neighbor, current.Distance + 1));
            }
        }

        return int.MaxValue;
    }

    private bool IsGhostTraversable(int x, int y) =>
        y >= 0 && y < grid.Length && x >= 0 && x < grid[0].Length && grid[y][x] != 1;

    private bool CanMove(double x, double y, string direction, bool isPacman)
    {
        var d = Directions[direction]; var tx = (int)x + d.X; var ty = (int)y + d.Y;
        if (ty == TunnelRow && (tx < 0 || tx >= grid[0].Length)) return true;
        return ty >= 0 && ty < grid.Length && tx >= 0 && tx < grid[0].Length && grid[ty][tx] != 1 && (!isPacman || grid[ty][tx] != 3);
    }

    private void Move(Actor actor)
    {
        var d = Directions[actor.Direction]; actor.X += d.X * actor.Speed; actor.Y += d.Y * actor.Speed;
        if (Math.Round(actor.Y) == TunnelRow) { if (actor.X < 0) actor.X += grid[0].Length; else if (actor.X >= grid[0].Length) actor.X -= grid[0].Length; }
    }
    private void ResetPositions()
    {
        pacman.X = PacmanStart.X;
        pacman.Y = PacmanStart.Y;
        pacman.Direction = "left";
        pacman.NextDirection = null;
        dotsEatenThisLife = 0;

        for (var i = 0; i < ghosts.Count; i++)
        {
            var ghost = ghosts[i];
            var start = GhostStarts[i];
            ghost.X = start.X;
            ghost.Y = start.Y;
            ghost.Direction = "up";
            ghost.Released = start.ReleaseScore == 0;
            ghost.LeavingHouse = ghost.Released;
            ghost.ExitPathIndex = 0;
            ghost.AiCycleStartedAt = DateTime.UtcNow;
        }
    }
    private static bool Aligned(Actor actor) => Math.Abs(actor.X - Math.Round(actor.X)) < .001 && Math.Abs(actor.Y - Math.Round(actor.Y)) < .001;
    private static void Snap(Actor actor) { actor.X = Math.Round(actor.X); actor.Y = Math.Round(actor.Y); }
    private sealed class Actor(double x, double y, string direction, double speed, string kind, (int X, int Y)[] exitPath)
    {
        public double X = x, Y = y;
        public string Direction = direction;
        public string? NextDirection;
        public double Speed = speed;
        public string Kind = kind;
        public bool Released;
        public bool LeavingHouse;
        public DateTime AiCycleStartedAt = DateTime.UtcNow;
        public (int X, int Y)[] ExitPath = exitPath;
        public int ExitPathIndex;
    }
}

public sealed record GameFrame(int[][] Grid, int Score, int Lives, string State, ActorFrame Pacman, ActorFrame[] Ghosts);
public sealed record ActorFrame(double X, double Y, string Direction);
