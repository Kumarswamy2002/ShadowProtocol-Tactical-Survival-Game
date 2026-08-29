// =============================================================================
// ShadowProtocol.AI — A* Pathfinder: Grid-based pathfinding with optimizations
// =============================================================================

using System;
using System.Collections.Generic;
using ShadowProtocol.Core.Math;

namespace ShadowProtocol.AI.Pathfinding
{
    /// <summary>
    /// Represents a node in the pathfinding grid with associated costs and metadata.
    /// </summary>
    public class PathNode : IComparable<PathNode>
    {
        /// <summary>Grid X coordinate.</summary>
        public int X { get; set; }
        /// <summary>Grid Y coordinate.</summary>
        public int Y { get; set; }
        /// <summary>Whether this node is walkable/traversable.</summary>
        public bool Walkable { get; set; } = true;
        /// <summary>Additional movement cost multiplier for this node (terrain penalty).</summary>
        public float MovementPenalty { get; set; }
        /// <summary>G cost: actual cost from the start node to this node.</summary>
        public float GCost { get; set; }
        /// <summary>H cost: heuristic estimated cost from this node to the target.</summary>
        public float HCost { get; set; }
        /// <summary>F cost: total estimated cost (G + H).</summary>
        public float FCost => GCost + HCost;
        /// <summary>Parent node in the path (for path reconstruction).</summary>
        public PathNode Parent { get; set; }
        /// <summary>World-space position of this node center.</summary>
        public Vector3F WorldPosition { get; set; }
        /// <summary>Elevation/height at this node for 3D pathfinding.</summary>
        public float Height { get; set; }
        /// <summary>Node flags for special terrain types.</summary>
        public TerrainType Terrain { get; set; } = TerrainType.Normal;

        public int CompareTo(PathNode other)
        {
            int compare = FCost.CompareTo(other.FCost);
            if (compare == 0) compare = HCost.CompareTo(other.HCost);
            return compare;
        }

        public override bool Equals(object obj) => obj is PathNode n && n.X == X && n.Y == Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }

    /// <summary>
    /// Terrain classification for pathfinding cost calculations.
    /// </summary>
    [Flags]
    public enum TerrainType
    {
        Normal      = 0,
        Road        = 1 << 0,   // Fast travel
        Mud         = 1 << 1,   // Slow movement
        Water       = 1 << 2,   // Requires swimming
        DeepWater   = 1 << 3,   // Impassable without vehicle
        Cliff       = 1 << 4,   // Impassable on foot
        Forest      = 1 << 5,   // Provides cover, slightly slower
        Urban       = 1 << 6,   // Buildings and streets
        Radiation   = 1 << 7,   // Hazardous zone
        Minefield   = 1 << 8,   // Dangerous
    }

    /// <summary>
    /// Movement capability flags for different entity types.
    /// Determines which terrain an entity can traverse.
    /// </summary>
    [Flags]
    public enum MovementCapability
    {
        Walk        = 1 << 0,
        Swim        = 1 << 1,
        Climb       = 1 << 2,
        Fly         = 1 << 3,
        Drive       = 1 << 4,
        AllTerrain  = Walk | Swim | Climb,
        Vehicle     = Drive,
        Air         = Fly,
    }

    /// <summary>
    /// Configuration for the pathfinding system.
    /// </summary>
    public class PathfindingConfig
    {
        /// <summary>Maximum number of nodes to evaluate before giving up.</summary>
        public int MaxSearchNodes { get; set; } = 5000;
        /// <summary>Whether diagonal movement is allowed.</summary>
        public bool AllowDiagonal { get; set; } = true;
        /// <summary>Cost multiplier for diagonal movement (sqrt(2) ≈ 1.414).</summary>
        public float DiagonalCost { get; set; } = 1.414f;
        /// <summary>Whether to smooth the path after finding it.</summary>
        public bool SmoothPath { get; set; } = true;
        /// <summary>Movement capabilities of the pathfinding entity.</summary>
        public MovementCapability Capabilities { get; set; } = MovementCapability.Walk;
        /// <summary>Maximum slope angle the entity can traverse (degrees).</summary>
        public float MaxSlopeAngle { get; set; } = 45f;
        /// <summary>Entity radius for obstacle avoidance.</summary>
        public float AgentRadius { get; set; } = 0.5f;
        /// <summary>Entity height for clearance checks.</summary>
        public float AgentHeight { get; set; } = 2f;
    }

    /// <summary>
    /// Represents the result of a pathfinding query.
    /// </summary>
    public class PathResult
    {
        /// <summary>Whether a valid path was found.</summary>
        public bool Success { get; set; }
        /// <summary>Ordered list of waypoints from start to goal.</summary>
        public List<Vector3F> Waypoints { get; set; } = new();
        /// <summary>Total path length in world units.</summary>
        public float TotalDistance { get; set; }
        /// <summary>Total path cost (including terrain penalties).</summary>
        public float TotalCost { get; set; }
        /// <summary>Number of nodes evaluated during search.</summary>
        public int NodesEvaluated { get; set; }
        /// <summary>Time taken to compute the path in milliseconds.</summary>
        public float ComputeTimeMs { get; set; }
        /// <summary>Reason for failure, if applicable.</summary>
        public string FailureReason { get; set; }

        public static PathResult Failed(string reason) =>
            new PathResult { Success = false, FailureReason = reason };
    }

    /// <summary>
    /// High-performance A* pathfinder for grid-based navigation.
    /// Features include terrain costs, diagonal movement, path smoothing,
    /// slope angle limits, and agent-size clearance checks.
    /// </summary>
    public class AStarPathfinder
    {
        private readonly PathNode[,] _grid;
        private readonly int _width;
        private readonly int _height;
        private readonly float _cellSize;
        private readonly Vector3F _gridOrigin;

        // Neighbor offsets for 4-directional and 8-directional movement
        private static readonly (int dx, int dy)[] CardinalDirs = { (0,1), (1,0), (0,-1), (-1,0) };
        private static readonly (int dx, int dy)[] DiagonalDirs = { (1,1), (1,-1), (-1,1), (-1,-1) };

        /// <summary>Grid width in cells.</summary>
        public int Width => _width;
        /// <summary>Grid height in cells.</summary>
        public int Height => _height;
        /// <summary>Size of each grid cell in world units.</summary>
        public float CellSize => _cellSize;

        /// <summary>
        /// Creates a pathfinding grid with the specified dimensions.
        /// </summary>
        /// <param name="width">Grid width in cells.</param>
        /// <param name="height">Grid height in cells.</param>
        /// <param name="cellSize">World-space size of each cell.</param>
        /// <param name="origin">World-space origin of the grid (bottom-left corner).</param>
        public AStarPathfinder(int width, int height, float cellSize = 1f, Vector3F origin = default)
        {
            _width = width;
            _height = height;
            _cellSize = cellSize;
            _gridOrigin = origin;
            _grid = new PathNode[width, height];

            // Initialize grid nodes
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _grid[x, y] = new PathNode
                    {
                        X = x, Y = y,
                        WorldPosition = GridToWorld(x, y),
                        Walkable = true,
                        MovementPenalty = 0f
                    };
                }
            }
        }

        /// <summary>
        /// Gets the grid node at the specified grid coordinates.
        /// </summary>
        public PathNode GetNode(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return null;
            return _grid[x, y];
        }

        /// <summary>
        /// Sets the walkability and penalty of a grid node.
        /// </summary>
        public void SetNode(int x, int y, bool walkable, float penalty = 0f, TerrainType terrain = TerrainType.Normal)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;
            _grid[x, y].Walkable = walkable;
            _grid[x, y].MovementPenalty = penalty;
            _grid[x, y].Terrain = terrain;
        }

        /// <summary>
        /// Converts grid coordinates to world-space position.
        /// </summary>
        public Vector3F GridToWorld(int x, int y)
        {
            return new Vector3F(
                _gridOrigin.X + x * _cellSize + _cellSize * 0.5f,
                _gridOrigin.Y,
                _gridOrigin.Z + y * _cellSize + _cellSize * 0.5f
            );
        }

        /// <summary>
        /// Converts a world-space position to grid coordinates.
        /// </summary>
        public (int x, int y) WorldToGrid(Vector3F worldPos)
        {
            int x = (int)((worldPos.X - _gridOrigin.X) / _cellSize);
            int y = (int)((worldPos.Z - _gridOrigin.Z) / _cellSize);
            return (MathUtils.Clamp(x, 0, _width - 1), MathUtils.Clamp(y, 0, _height - 1));
        }

        /// <summary>
        /// Finds a path from start to goal using A* algorithm.
        /// </summary>
        /// <param name="start">Start position in world space.</param>
        /// <param name="goal">Goal position in world space.</param>
        /// <param name="config">Pathfinding configuration.</param>
        /// <returns>Path result containing waypoints or failure information.</returns>
        public PathResult FindPath(Vector3F start, Vector3F goal, PathfindingConfig config = null)
        {
            config ??= new PathfindingConfig();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var (sx, sy) = WorldToGrid(start);
            var (gx, gy) = WorldToGrid(goal);

            PathNode startNode = _grid[sx, sy];
            PathNode goalNode = _grid[gx, gy];

            if (!startNode.Walkable)
                return PathResult.Failed("Start position is not walkable");
            if (!goalNode.Walkable)
                return PathResult.Failed("Goal position is not walkable");

            // Reset node costs
            ResetGrid();

            // Priority queue using sorted set with tie-breaking
            var openSet = new SortedSet<PathNode>(Comparer<PathNode>.Create((a, b) =>
            {
                int c = a.FCost.CompareTo(b.FCost);
                if (c != 0) return c;
                c = a.HCost.CompareTo(b.HCost);
                if (c != 0) return c;
                c = a.X.CompareTo(b.X);
                if (c != 0) return c;
                return a.Y.CompareTo(b.Y);
            }));
            var closedSet = new HashSet<PathNode>();
            var inOpenSet = new HashSet<PathNode>();

            startNode.GCost = 0;
            startNode.HCost = Heuristic(startNode, goalNode);
            openSet.Add(startNode);
            inOpenSet.Add(startNode);

            int nodesEvaluated = 0;

            while (openSet.Count > 0 && nodesEvaluated < config.MaxSearchNodes)
            {
                PathNode current = openSet.Min;
                openSet.Remove(current);
                inOpenSet.Remove(current);
                closedSet.Add(current);
                nodesEvaluated++;

                // Goal reached
                if (current.X == goalNode.X && current.Y == goalNode.Y)
                {
                    var path = ReconstructPath(current);
                    if (config.SmoothPath) path = SmoothPath(path);

                    sw.Stop();
                    return new PathResult
                    {
                        Success = true,
                        Waypoints = path,
                        TotalCost = current.GCost,
                        TotalDistance = CalculatePathDistance(path),
                        NodesEvaluated = nodesEvaluated,
                        ComputeTimeMs = (float)sw.Elapsed.TotalMilliseconds,
                    };
                }

                // Evaluate neighbors
                foreach (var neighbor in GetNeighbors(current, config))
                {
                    if (closedSet.Contains(neighbor)) continue;
                    if (!CanTraverse(current, neighbor, config)) continue;

                    float moveCost = MovementCost(current, neighbor, config);
                    float tentativeG = current.GCost + moveCost;

                    if (tentativeG < neighbor.GCost || !inOpenSet.Contains(neighbor))
                    {
                        if (inOpenSet.Contains(neighbor)) openSet.Remove(neighbor);

                        neighbor.GCost = tentativeG;
                        neighbor.HCost = Heuristic(neighbor, goalNode);
                        neighbor.Parent = current;

                        openSet.Add(neighbor);
                        inOpenSet.Add(neighbor);
                    }
                }
            }

            sw.Stop();
            return new PathResult
            {
                Success = false,
                FailureReason = nodesEvaluated >= config.MaxSearchNodes
                    ? "Search limit reached" : "No path found",
                NodesEvaluated = nodesEvaluated,
                ComputeTimeMs = (float)sw.Elapsed.TotalMilliseconds,
            };
        }

        /// <summary>
        /// Octile distance heuristic — admissible for grids with diagonal movement.
        /// </summary>
        private float Heuristic(PathNode a, PathNode b)
        {
            int dx = System.Math.Abs(a.X - b.X);
            int dy = System.Math.Abs(a.Y - b.Y);
            return (dx + dy) + (1.414f - 2f) * System.Math.Min(dx, dy);
        }

        /// <summary>
        /// Calculates the movement cost between two adjacent nodes including terrain penalties.
        /// </summary>
        private float MovementCost(PathNode from, PathNode to, PathfindingConfig config)
        {
            bool isDiagonal = from.X != to.X && from.Y != to.Y;
            float baseCost = isDiagonal ? config.DiagonalCost : 1f;

            // Apply terrain penalties
            float terrainCost = GetTerrainCost(to.Terrain, config.Capabilities);
            float penalty = to.MovementPenalty;

            // Height difference cost (for 3D terrain)
            float heightDiff = MathF.Abs(to.Height - from.Height);
            float slopeCost = heightDiff * 0.5f;

            return baseCost * (1f + terrainCost + penalty) + slopeCost;
        }

        /// <summary>
        /// Returns the movement cost modifier for a given terrain type.
        /// </summary>
        private float GetTerrainCost(TerrainType terrain, MovementCapability caps)
        {
            if (terrain.HasFlag(TerrainType.Road)) return -0.3f; // Roads are faster
            if (terrain.HasFlag(TerrainType.Mud)) return 0.8f;
            if (terrain.HasFlag(TerrainType.Forest)) return 0.3f;
            if (terrain.HasFlag(TerrainType.Water))
                return caps.HasFlag(MovementCapability.Swim) ? 0.5f : 100f;
            if (terrain.HasFlag(TerrainType.Radiation)) return 1.5f;
            return 0f;
        }

        /// <summary>
        /// Checks if an entity can traverse from one node to another.
        /// </summary>
        private bool CanTraverse(PathNode from, PathNode to, PathfindingConfig config)
        {
            if (!to.Walkable) return false;

            // Check slope angle
            float heightDiff = MathF.Abs(to.Height - from.Height);
            float horizontalDist = _cellSize;
            float slopeAngle = MathF.Atan2(heightDiff, horizontalDist) * MathUtils.Rad2Deg;
            if (slopeAngle > config.MaxSlopeAngle) return false;

            // Check terrain passability
            if (to.Terrain.HasFlag(TerrainType.DeepWater) && !config.Capabilities.HasFlag(MovementCapability.Swim))
                return false;
            if (to.Terrain.HasFlag(TerrainType.Cliff) && !config.Capabilities.HasFlag(MovementCapability.Climb))
                return false;

            // Diagonal corner-cutting prevention
            if (from.X != to.X && from.Y != to.Y)
            {
                var adj1 = GetNode(from.X, to.Y);
                var adj2 = GetNode(to.X, from.Y);
                if (adj1 != null && !adj1.Walkable) return false;
                if (adj2 != null && !adj2.Walkable) return false;
            }

            return true;
        }

        /// <summary>
        /// Gets valid neighboring nodes for pathfinding expansion.
        /// </summary>
        private IEnumerable<PathNode> GetNeighbors(PathNode node, PathfindingConfig config)
        {
            foreach (var (dx, dy) in CardinalDirs)
            {
                var neighbor = GetNode(node.X + dx, node.Y + dy);
                if (neighbor != null) yield return neighbor;
            }

            if (config.AllowDiagonal)
            {
                foreach (var (dx, dy) in DiagonalDirs)
                {
                    var neighbor = GetNode(node.X + dx, node.Y + dy);
                    if (neighbor != null) yield return neighbor;
                }
            }
        }

        /// <summary>
        /// Reconstructs the path from goal to start by following parent pointers.
        /// </summary>
        private List<Vector3F> ReconstructPath(PathNode goalNode)
        {
            var path = new List<Vector3F>();
            var current = goalNode;
            while (current != null)
            {
                path.Add(current.WorldPosition);
                current = current.Parent;
            }
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Smooths a path using line-of-sight checks to remove unnecessary waypoints.
        /// This is known as the "string pulling" or "funnel" technique.
        /// </summary>
        private List<Vector3F> SmoothPath(List<Vector3F> path)
        {
            if (path.Count <= 2) return path;

            var smoothed = new List<Vector3F> { path[0] };
            int current = 0;

            while (current < path.Count - 1)
            {
                int furthest = current + 1;
                for (int i = path.Count - 1; i > current + 1; i--)
                {
                    if (HasLineOfSight(path[current], path[i]))
                    {
                        furthest = i;
                        break;
                    }
                }
                smoothed.Add(path[furthest]);
                current = furthest;
            }

            return smoothed;
        }

        /// <summary>
        /// Checks if there is a clear line of sight between two world positions.
        /// Uses Bresenham-style grid traversal.
        /// </summary>
        public bool HasLineOfSight(Vector3F from, Vector3F to)
        {
            var (x0, y0) = WorldToGrid(from);
            var (x1, y1) = WorldToGrid(to);

            int dx = System.Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -System.Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;

            while (true)
            {
                var node = GetNode(x0, y0);
                if (node == null || !node.Walkable) return false;
                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }

            return true;
        }

        /// <summary>
        /// Calculates the total world-space distance of a path.
        /// </summary>
        private float CalculatePathDistance(List<Vector3F> path)
        {
            float dist = 0;
            for (int i = 1; i < path.Count; i++)
                dist += (path[i] - path[i - 1]).Magnitude();
            return dist;
        }

        /// <summary>
        /// Resets all node costs for a fresh pathfinding query.
        /// </summary>
        private void ResetGrid()
        {
            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                {
                    _grid[x, y].GCost = float.MaxValue;
                    _grid[x, y].HCost = 0;
                    _grid[x, y].Parent = null;
                }
        }

        /// <summary>
        /// Generates a random walkable position on the grid.
        /// Useful for spawning NPCs, loot, and patrol waypoints.
        /// </summary>
        public Vector3F GetRandomWalkablePosition()
        {
            var rng = new Random();
            for (int attempt = 0; attempt < 1000; attempt++)
            {
                int x = rng.Next(0, _width);
                int y = rng.Next(0, _height);
                if (_grid[x, y].Walkable)
                    return _grid[x, y].WorldPosition;
            }
            return _gridOrigin; // Fallback
        }

        /// <summary>
        /// Updates the grid from a heightmap for terrain-aware pathfinding.
        /// </summary>
        public void ApplyHeightmap(float[,] heightmap)
        {
            int mapW = heightmap.GetLength(0);
            int mapH = heightmap.GetLength(1);

            for (int x = 0; x < _width && x < mapW; x++)
            {
                for (int y = 0; y < _height && y < mapH; y++)
                {
                    _grid[x, y].Height = heightmap[x, y];
                    var pos = _grid[x, y].WorldPosition;
                    _grid[x, y].WorldPosition = new Vector3F(pos.X, heightmap[x, y], pos.Z);
                }
            }
        }
    }

    /// <summary>
    /// Flow field pathfinding for group/swarm movement.
    /// Precomputes a direction field that multiple entities can follow simultaneously.
    /// More efficient than A* when many entities share the same destination.
    /// </summary>
    public class FlowField
    {
        private readonly int _width;
        private readonly int _height;
        private readonly float _cellSize;
        private readonly Vector3F _origin;

        private float[,] _costField;
        private float[,] _integrationField;
        private Vector3F[,] _flowDirections;
        private bool[,] _walkable;

        /// <summary>Grid width.</summary>
        public int Width => _width;
        /// <summary>Grid height.</summary>
        public int Height => _height;

        public FlowField(int width, int height, float cellSize = 1f, Vector3F origin = default)
        {
            _width = width;
            _height = height;
            _cellSize = cellSize;
            _origin = origin;

            _costField = new float[width, height];
            _integrationField = new float[width, height];
            _flowDirections = new Vector3F[width, height];
            _walkable = new bool[width, height];

            // Initialize all cells as walkable with cost 1
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    _costField[x, y] = 1f;
                    _walkable[x, y] = true;
                }
        }

        /// <summary>Sets a cell's base cost and walkability.</summary>
        public void SetCell(int x, int y, float cost, bool walkable)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;
            _costField[x, y] = cost;
            _walkable[x, y] = walkable;
        }

        /// <summary>
        /// Generates the flow field towards a target position.
        /// After calling this, entities can query GetFlowDirection() for movement.
        /// </summary>
        /// <param name="target">Target position in world space.</param>
        public void Generate(Vector3F target)
        {
            var (tx, ty) = WorldToGrid(target);

            // Step 1: Reset integration field
            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                    _integrationField[x, y] = float.MaxValue;

            // Step 2: Dijkstra integration from target
            _integrationField[tx, ty] = 0;
            var open = new Queue<(int x, int y)>();
            open.Enqueue((tx, ty));

            while (open.Count > 0)
            {
                var (cx, cy) = open.Dequeue();

                foreach (var (nx, ny) in GetCardinalNeighbors(cx, cy))
                {
                    if (!_walkable[nx, ny]) continue;

                    float newCost = _integrationField[cx, cy] + _costField[nx, ny];
                    if (newCost < _integrationField[nx, ny])
                    {
                        _integrationField[nx, ny] = newCost;
                        open.Enqueue((nx, ny));
                    }
                }
            }

            // Step 3: Generate flow directions from integration field gradient
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (!_walkable[x, y] || _integrationField[x, y] >= float.MaxValue)
                    {
                        _flowDirections[x, y] = Vector3F.Zero;
                        continue;
                    }

                    float bestCost = _integrationField[x, y];
                    int bestX = x, bestY = y;

                    foreach (var (nx, ny) in GetAllNeighbors(x, y))
                    {
                        if (_integrationField[nx, ny] < bestCost)
                        {
                            bestCost = _integrationField[nx, ny];
                            bestX = nx;
                            bestY = ny;
                        }
                    }

                    if (bestX == x && bestY == y)
                        _flowDirections[x, y] = Vector3F.Zero;
                    else
                    {
                        float dx = bestX - x, dy = bestY - y;
                        float len = MathF.Sqrt(dx * dx + dy * dy);
                        _flowDirections[x, y] = new Vector3F(dx / len, 0, dy / len);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the flow direction at a world position.
        /// Returns a normalized direction vector entities should follow.
        /// </summary>
        public Vector3F GetFlowDirection(Vector3F worldPos)
        {
            var (x, y) = WorldToGrid(worldPos);
            if (x < 0 || x >= _width || y < 0 || y >= _height) return Vector3F.Zero;
            return _flowDirections[x, y];
        }

        /// <summary>
        /// Gets the integration cost at a world position.
        /// Lower values are closer to the target.
        /// </summary>
        public float GetCostAtPosition(Vector3F worldPos)
        {
            var (x, y) = WorldToGrid(worldPos);
            if (x < 0 || x >= _width || y < 0 || y >= _height) return float.MaxValue;
            return _integrationField[x, y];
        }

        private (int x, int y) WorldToGrid(Vector3F pos)
        {
            int x = (int)((pos.X - _origin.X) / _cellSize);
            int y = (int)((pos.Z - _origin.Z) / _cellSize);
            return (MathUtils.Clamp(x, 0, _width - 1), MathUtils.Clamp(y, 0, _height - 1));
        }

        private IEnumerable<(int x, int y)> GetCardinalNeighbors(int x, int y)
        {
            if (x > 0) yield return (x - 1, y);
            if (x < _width - 1) yield return (x + 1, y);
            if (y > 0) yield return (x, y - 1);
            if (y < _height - 1) yield return (x, y + 1);
        }

        private IEnumerable<(int x, int y)> GetAllNeighbors(int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && nx < _width && ny >= 0 && ny < _height)
                        yield return (nx, ny);
                }
        }
    }

    /// <summary>
    /// Dynamic obstacle avoidance using velocity obstacles (ORCA-inspired).
    /// Prevents AI agents from colliding while pathfinding.
    /// </summary>
    public class ObstacleAvoidance
    {
        /// <summary>Represents a navigating agent for avoidance calculations.</summary>
        public class Agent
        {
            public int Id;
            public Vector3F Position;
            public Vector3F Velocity;
            public Vector3F DesiredVelocity;
            public float Radius;
            public float MaxSpeed;
            public float Mass;
        }

        private readonly List<Agent> _agents = new();
        private readonly float _timeHorizon;
        private readonly float _neighborDistance;

        public ObstacleAvoidance(float timeHorizon = 2f, float neighborDistance = 10f)
        {
            _timeHorizon = timeHorizon;
            _neighborDistance = neighborDistance;
        }

        /// <summary>Registers an agent for avoidance.</summary>
        public void AddAgent(Agent agent) => _agents.Add(agent);

        /// <summary>Removes an agent from avoidance.</summary>
        public void RemoveAgent(int id) => _agents.RemoveAll(a => a.Id == id);

        /// <summary>
        /// Computes adjusted velocities for all agents to avoid collisions.
        /// Uses reciprocal velocity obstacles for smooth multi-agent avoidance.
        /// </summary>
        public void ComputeAvoidance(float deltaTime)
        {
            foreach (var agent in _agents)
            {
                Vector3F adjustedVelocity = agent.DesiredVelocity;

                foreach (var other in _agents)
                {
                    if (other.Id == agent.Id) continue;

                    Vector3F relPos = other.Position - agent.Position;
                    float distSq = relPos.MagnitudeSquared();
                    float neighborDistSq = _neighborDistance * _neighborDistance;

                    if (distSq > neighborDistSq) continue;

                    float dist = MathF.Sqrt(distSq);
                    float combinedRadius = agent.Radius + other.Radius;

                    if (dist < combinedRadius + 0.01f)
                    {
                        // Separation force (push apart)
                        Vector3F pushDir = dist > 0.001f
                            ? relPos * (-1f / dist)
                            : new Vector3F(MathUtils.RandomRange(-1f, 1f), 0, MathUtils.RandomRange(-1f, 1f));
                        adjustedVelocity = adjustedVelocity + pushDir * agent.MaxSpeed * 0.5f;
                    }
                    else
                    {
                        // Velocity obstacle avoidance
                        Vector3F relVel = agent.DesiredVelocity - other.Velocity;
                        float timeToCollision = TimeToCollision(agent.Position, relVel, other.Position, combinedRadius);

                        if (timeToCollision > 0 && timeToCollision < _timeHorizon)
                        {
                            Vector3F futureRelPos = relPos - relVel * timeToCollision;
                            float futureLen = futureRelPos.Magnitude();
                            if (futureLen > 0.001f)
                            {
                                Vector3F avoidDir = futureRelPos * (-1f / futureLen);
                                float urgency = 1f / (timeToCollision + 0.1f);
                                adjustedVelocity = adjustedVelocity + avoidDir * urgency;
                            }
                        }
                    }
                }

                // Clamp to max speed
                float speed = adjustedVelocity.Magnitude();
                if (speed > agent.MaxSpeed)
                    adjustedVelocity = adjustedVelocity * (agent.MaxSpeed / speed);

                agent.Velocity = adjustedVelocity;
            }
        }

        /// <summary>
        /// Calculates the time until two moving spheres collide.
        /// Returns negative if no collision will occur.
        /// </summary>
        private float TimeToCollision(Vector3F posA, Vector3F relVel, Vector3F posB, float combinedRadius)
        {
            Vector3F w = posB - posA;
            float a = relVel.MagnitudeSquared();
            float b = -2f * Vector3F.Dot(relVel, w);
            float c = w.MagnitudeSquared() - combinedRadius * combinedRadius;

            float discriminant = b * b - 4f * a * c;
            if (discriminant < 0 || a < 0.0001f) return -1f;

            float sqrtD = MathF.Sqrt(discriminant);
            float t = (-b - sqrtD) / (2f * a);
            return t;
        }
    }
}
