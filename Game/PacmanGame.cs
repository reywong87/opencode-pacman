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
    private static readonly (int X, int Y) PacmanStart = (13, 23);
    private static readonly (int X, int Y, string Kind, int ReleaseScore)[] GhostStarts =
    [
        (13, 14, "red", 0),
        (14, 14, "pink", 100),
        (13, 15, "cyan", 300),
        (14, 15, "orange", 600)
    ];
    private static readonly string[] MazeRows =
    [
        "############################", "#............##............#", "#.####.#####.##.#####.####.#", "#.####.#####.##.#####.####.#", "#.####.#####.##.#####.####.#", "#..........................#", "#.####.##.########.##.####.#", "#.####.##.########.##.####.#", "#......##....##....##......#", "######.#####.##.#####.######", "######.#####.##.#####.######", "######.##..........##.######", "######.##.###--###.##.######", "######.##.#      #.##.######", "          #      #          ", "######.##.#      #.##.######", "######.##.########.##.######", "######.##..........##.######", "######.#####.##.#####.######", "######.#####.##.#####.######", "#............##............#", "#.####.#####.##.#####.####.#", "#.####.#####.##.#####.####.#", "#...##................##...#", "###.##.##.########.##.##.###", "###.##.##.########.##.##.###", "#......##....##....##......#", "#.##########.##.##########.#", "#.##########.##.##########.#", "#..........................#", "############################"
    ];

    private int[][] grid = [];
    private Actor pacman = null!;
    private List<Actor> ghosts = [];
    private int score, lives, dots;
    public string State { get; private set; } = "start";

    public PacmanGame() => Reset("start");
    public GameFrame Frame => new(grid, score, lives, State, new(pacman.X, pacman.Y, pacman.Direction), ghosts.Select(g => new ActorFrame(g.X, g.Y, g.Direction)).ToArray());

    public void Reset() => Reset("playing");
    public void SetDirection(string direction) { if (State == "playing" && Directions.ContainsKey(direction)) pacman.NextDirection = direction; }

    public void Update()
    {
        if (State != "playing") return;
        MovePacman();
        foreach (var ghost in ghosts) MoveGhost(ghost);
        if (ghosts.Any(g => Math.Abs(g.X - pacman.X) < .5 && Math.Abs(g.Y - pacman.Y) < .5))
        {
            if (--lives == 0) { State = "lost"; return; }
            ResetPositions();
        }
        if (dots == 0) State = "won";
    }

    private void Reset(string state)
    {
        grid = MazeRows.Select(row => row.Select(c => c == '#' ? 1 : c == '.' ? 2 : c == '-' ? 3 : 0).ToArray()).ToArray();
        grid[PacmanStart.Y][PacmanStart.X] = 0;
        dots = grid.Sum(row => row.Count(cell => cell == 2)); score = 0; lives = 3; State = state;
        pacman = new(PacmanStart.X, PacmanStart.Y, "left", PacmanSpeed, "pacman");
        ghosts = GhostStarts.Select(g => new Actor(g.X, g.Y, "up", GhostSpeed, g.Kind)).ToList();
    }

    private void MovePacman()
    {
        if (Aligned(pacman))
        {
            Snap(pacman);
            if (pacman.NextDirection is { } next && CanMove(pacman.X, pacman.Y, next, true)) { pacman.Direction = next; pacman.NextDirection = null; }
            if (grid[(int)pacman.Y][(int)pacman.X] == 2) { grid[(int)pacman.Y][(int)pacman.X] = 0; score += 10; dots--; }
            if (!CanMove(pacman.X, pacman.Y, pacman.Direction, true)) return;
        }
        Move(pacman);
    }

    private void MoveGhost(Actor ghost)
    {
        if (Aligned(ghost)) { Snap(ghost); DecideGhost(ghost); if (!CanMove(ghost.X, ghost.Y, ghost.Direction, false)) return; }
        Move(ghost);
    }

    private void DecideGhost(Actor ghost)
    {
        var choices = Directions.Keys.Where(d => d != Opposite[ghost.Direction] && CanMove(ghost.X, ghost.Y, d, false)).ToArray();
        if (choices.Length == 0) choices = [Opposite[ghost.Direction]];
        ghost.Direction = ghost.Kind == "hunter"
            ? choices.MinBy(d => Math.Abs(ghost.X + Directions[d].X - Math.Round(pacman.X)) + Math.Abs(ghost.Y + Directions[d].Y - Math.Round(pacman.Y)))!
            : choices[Random.Shared.Next(choices.Length)];
    }

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
    private void ResetPositions() { pacman.X = PacmanStart.X; pacman.Y = PacmanStart.Y; pacman.Direction = "left"; pacman.NextDirection = null; for (var i = 0; i < ghosts.Count; i++) { ghosts[i].X = GhostStarts[i].X; ghosts[i].Y = GhostStarts[i].Y; ghosts[i].Direction = "up"; } }
    private static bool Aligned(Actor actor) => Math.Abs(actor.X - Math.Round(actor.X)) < .001 && Math.Abs(actor.Y - Math.Round(actor.Y)) < .001;
    private static void Snap(Actor actor) { actor.X = Math.Round(actor.X); actor.Y = Math.Round(actor.Y); }
    private sealed class Actor(double x, double y, string direction, double speed, string kind)
    {
        public double X = x, Y = y;
        public string Direction = direction;
        public string? NextDirection;
        public double Speed = speed;
        public string Kind = kind;
        public bool Released;
        public bool LeavingHouse;
        public DateTime AiCycleStartedAt = DateTime.UtcNow;
    }
}

public sealed record GameFrame(int[][] Grid, int Score, int Lives, string State, ActorFrame Pacman, ActorFrame[] Ghosts);
public sealed record ActorFrame(double X, double Y, string Direction);
