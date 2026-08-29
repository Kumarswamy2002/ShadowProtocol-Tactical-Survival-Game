// =============================================================================
// ShadowProtocol.AI — GOAP: Goal-Oriented Action Planning System
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using ShadowProtocol.Core.Math;

namespace ShadowProtocol.AI.GOAP
{
    /// <summary>
    /// Represents a world state as a dictionary of named boolean/numeric conditions.
    /// GOAP plans are found by searching from a goal state back to the current world state.
    /// </summary>
    public class WorldState
    {
        private readonly Dictionary<string, object> _values = new();

        /// <summary>Gets a boolean value from the world state.</summary>
        public bool GetBool(string key, bool defaultValue = false)
        {
            if (_values.TryGetValue(key, out var val) && val is bool b) return b;
            return defaultValue;
        }

        /// <summary>Gets a numeric value from the world state.</summary>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            if (_values.TryGetValue(key, out var val))
            {
                if (val is float f) return f;
                if (val is int i) return i;
                if (val is double d) return (float)d;
            }
            return defaultValue;
        }

        /// <summary>Gets an integer value from the world state.</summary>
        public int GetInt(string key, int defaultValue = 0)
        {
            if (_values.TryGetValue(key, out var val) && val is int i) return i;
            return defaultValue;
        }

        /// <summary>Sets a value in the world state.</summary>
        public void Set(string key, object value) => _values[key] = value;

        /// <summary>Checks if a key exists in the world state.</summary>
        public bool Has(string key) => _values.ContainsKey(key);

        /// <summary>
        /// Checks if this world state satisfies all conditions in the goal state.
        /// </summary>
        public bool Satisfies(WorldState goal)
        {
            foreach (var kvp in goal._values)
            {
                if (!_values.TryGetValue(kvp.Key, out var myVal))
                    return false;
                if (!Equals(myVal, kvp.Value))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the number of unsatisfied conditions relative to a goal.
        /// Used as a heuristic for A* planning.
        /// </summary>
        public int UnsatisfiedCount(WorldState goal)
        {
            int count = 0;
            foreach (var kvp in goal._values)
            {
                if (!_values.TryGetValue(kvp.Key, out var myVal) || !Equals(myVal, kvp.Value))
                    count++;
            }
            return count;
        }

        /// <summary>Creates a deep copy of this world state.</summary>
        public WorldState Clone()
        {
            var clone = new WorldState();
            foreach (var kvp in _values)
                clone._values[kvp.Key] = kvp.Value;
            return clone;
        }

        /// <summary>Applies effects (key-value changes) to this world state.</summary>
        public void ApplyEffects(Dictionary<string, object> effects)
        {
            foreach (var kvp in effects)
                _values[kvp.Key] = kvp.Value;
        }

        public override string ToString()
        {
            return "{" + string.Join(", ", _values.Select(kv => $"{kv.Key}={kv.Value}")) + "}";
        }
    }

    /// <summary>
    /// Represents an action that an AI agent can perform.
    /// Each action has preconditions (what must be true to use it),
    /// effects (how it changes the world state), and a cost.
    /// </summary>
    public abstract class GOAPAction
    {
        /// <summary>Unique name identifier for this action.</summary>
        public abstract string Name { get; }

        /// <summary>Base cost of performing this action.</summary>
        public virtual float Cost => 1f;

        /// <summary>Whether this action requires a specific target entity or position.</summary>
        public virtual bool RequiresTarget => false;

        /// <summary>Target position for this action (if applicable).</summary>
        public Vector3F TargetPosition { get; set; }

        /// <summary>Range within which this action can be performed.</summary>
        public virtual float ActionRange => 2f;

        /// <summary>Time required to perform this action in seconds.</summary>
        public virtual float Duration => 1f;

        /// <summary>Whether this action can be interrupted.</summary>
        public virtual bool Interruptible => true;

        /// <summary>
        /// Returns the preconditions that must be satisfied in the world state
        /// before this action can be performed.
        /// </summary>
        public abstract Dictionary<string, object> GetPreconditions();

        /// <summary>
        /// Returns the effects this action has on the world state when completed.
        /// </summary>
        public abstract Dictionary<string, object> GetEffects();

        /// <summary>
        /// Dynamically checks if this action is currently valid given the full world state.
        /// Called during planning to filter out contextually invalid actions.
        /// </summary>
        public virtual bool IsValid(WorldState state) => true;

        /// <summary>
        /// Returns the dynamic cost of this action given the current world state.
        /// Override to make costs context-dependent (e.g., distance-based).
        /// </summary>
        public virtual float GetDynamicCost(WorldState state) => Cost;

        /// <summary>Called when this action begins execution.</summary>
        public virtual void OnStart() { }

        /// <summary>Called every frame during execution. Return true when complete.</summary>
        public virtual bool OnUpdate(float deltaTime) => true;

        /// <summary>Called when this action is interrupted or cancelled.</summary>
        public virtual void OnCancel() { }

        /// <summary>Called when this action completes successfully.</summary>
        public virtual void OnComplete() { }

        public override string ToString() => $"[GOAP] {Name} (cost: {Cost})";
    }

    /// <summary>
    /// A node in the GOAP planning search graph.
    /// </summary>
    internal class PlanNode : IComparable<PlanNode>
    {
        public WorldState State;
        public GOAPAction Action;
        public PlanNode Parent;
        public float GCost;
        public float HCost;
        public float FCost => GCost + HCost;

        public int CompareTo(PlanNode other)
        {
            int c = FCost.CompareTo(other.FCost);
            if (c != 0) return c;
            return HCost.CompareTo(other.HCost);
        }
    }

    /// <summary>
    /// The result of a GOAP planning query.
    /// </summary>
    public class Plan
    {
        /// <summary>Whether a valid plan was found.</summary>
        public bool Success { get; set; }
        /// <summary>Ordered list of actions to execute.</summary>
        public List<GOAPAction> Actions { get; set; } = new();
        /// <summary>Total cost of the plan.</summary>
        public float TotalCost { get; set; }
        /// <summary>Estimated total duration of the plan.</summary>
        public float EstimatedDuration { get; set; }
        /// <summary>Number of nodes evaluated during planning.</summary>
        public int NodesEvaluated { get; set; }
        /// <summary>Planning computation time in ms.</summary>
        public float PlanTimeMs { get; set; }
        /// <summary>The goal state this plan achieves.</summary>
        public WorldState GoalState { get; set; }

        public static Plan Failed() => new Plan { Success = false };

        public override string ToString()
        {
            if (!Success) return "[Plan FAILED]";
            return $"[Plan OK] {Actions.Count} actions, cost: {TotalCost:F1}, " +
                   $"duration: {EstimatedDuration:F1}s\n" +
                   string.Join(" → ", Actions.Select(a => a.Name));
        }
    }

    /// <summary>
    /// GOAP Planner — finds optimal action sequences to achieve goals.
    /// Uses backwards A* search from goal state to current state.
    /// </summary>
    public class GOAPPlanner
    {
        private readonly List<GOAPAction> _availableActions = new();
        private int _maxPlanLength = 10;
        private int _maxSearchNodes = 1000;

        /// <summary>Maximum actions in a single plan.</summary>
        public int MaxPlanLength
        {
            get => _maxPlanLength;
            set => _maxPlanLength = value;
        }

        /// <summary>Registers an action as available for planning.</summary>
        public void AddAction(GOAPAction action) => _availableActions.Add(action);

        /// <summary>Removes an action from the available set.</summary>
        public void RemoveAction(string name) => _availableActions.RemoveAll(a => a.Name == name);

        /// <summary>Clears all registered actions.</summary>
        public void ClearActions() => _availableActions.Clear();

        /// <summary>
        /// Creates a plan to achieve the specified goal from the current world state.
        /// Uses backward A* search: starts from goal and works backward to find
        /// a sequence of actions that bridges from current state to goal state.
        /// </summary>
        public Plan CreatePlan(WorldState currentState, WorldState goalState)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Check if goal is already satisfied
            if (currentState.Satisfies(goalState))
            {
                sw.Stop();
                return new Plan
                {
                    Success = true,
                    TotalCost = 0,
                    PlanTimeMs = (float)sw.Elapsed.TotalMilliseconds,
                    GoalState = goalState,
                };
            }

            // Forward A* search
            var startNode = new PlanNode
            {
                State = currentState.Clone(),
                GCost = 0,
                HCost = currentState.UnsatisfiedCount(goalState),
            };

            var openSet = new SortedSet<PlanNode>(Comparer<PlanNode>.Create((a, b) =>
            {
                int c = a.FCost.CompareTo(b.FCost);
                if (c != 0) return c;
                return a.GetHashCode().CompareTo(b.GetHashCode());
            }));

            openSet.Add(startNode);
            int nodesEvaluated = 0;

            while (openSet.Count > 0 && nodesEvaluated < _maxSearchNodes)
            {
                var current = openSet.Min;
                openSet.Remove(current);
                nodesEvaluated++;

                // Check if goal is reached
                if (current.State.Satisfies(goalState))
                {
                    var plan = ReconstructPlan(current);
                    sw.Stop();
                    plan.NodesEvaluated = nodesEvaluated;
                    plan.PlanTimeMs = (float)sw.Elapsed.TotalMilliseconds;
                    plan.GoalState = goalState;
                    return plan;
                }

                // Count plan depth
                int depth = 0;
                var temp = current;
                while (temp.Parent != null) { depth++; temp = temp.Parent; }
                if (depth >= _maxPlanLength) continue;

                // Try all available actions
                foreach (var action in _availableActions)
                {
                    if (!action.IsValid(current.State)) continue;

                    // Check if preconditions are met
                    var preconditions = action.GetPreconditions();
                    bool preconditionsMet = true;
                    foreach (var pre in preconditions)
                    {
                        if (!current.State.Has(pre.Key) || !Equals(GetStateValue(current.State, pre.Key), pre.Value))
                        {
                            preconditionsMet = false;
                            break;
                        }
                    }

                    if (!preconditionsMet) continue;

                    // Apply action effects to create new state
                    var newState = current.State.Clone();
                    newState.ApplyEffects(action.GetEffects());

                    float actionCost = action.GetDynamicCost(current.State);
                    var newNode = new PlanNode
                    {
                        State = newState,
                        Action = action,
                        Parent = current,
                        GCost = current.GCost + actionCost,
                        HCost = newState.UnsatisfiedCount(goalState),
                    };

                    openSet.Add(newNode);
                }
            }

            sw.Stop();
            return new Plan
            {
                Success = false,
                NodesEvaluated = nodesEvaluated,
                PlanTimeMs = (float)sw.Elapsed.TotalMilliseconds,
            };
        }

        private object GetStateValue(WorldState state, string key)
        {
            if (state.GetBool(key)) return true;
            float f = state.GetFloat(key);
            if (f != 0) return f;
            return state.GetInt(key);
        }

        private Plan ReconstructPlan(PlanNode goalNode)
        {
            var actions = new List<GOAPAction>();
            var current = goalNode;
            float totalCost = 0;
            float totalDuration = 0;

            while (current.Parent != null)
            {
                if (current.Action != null)
                {
                    actions.Add(current.Action);
                    totalCost += current.Action.Cost;
                    totalDuration += current.Action.Duration;
                }
                current = current.Parent;
            }

            actions.Reverse();

            return new Plan
            {
                Success = true,
                Actions = actions,
                TotalCost = totalCost,
                EstimatedDuration = totalDuration,
            };
        }
    }

    /// <summary>
    /// Executes a GOAP plan by running actions sequentially.
    /// Handles action lifecycle (start, update, complete, cancel) and
    /// replanning when the world state changes unexpectedly.
    /// </summary>
    public class PlanExecutor
    {
        private Plan _currentPlan;
        private int _currentActionIndex;
        private GOAPAction _activeAction;
        private bool _isExecuting;
        private float _actionTimer;

        /// <summary>Whether a plan is currently being executed.</summary>
        public bool IsExecuting => _isExecuting;
        /// <summary>Current action being performed.</summary>
        public GOAPAction CurrentAction => _activeAction;
        /// <summary>Progress through the current plan (0..1).</summary>
        public float Progress => _currentPlan != null && _currentPlan.Actions.Count > 0
            ? (float)_currentActionIndex / _currentPlan.Actions.Count : 0f;

        /// <summary>Event raised when a plan completes successfully.</summary>
        public event Action<Plan> OnPlanComplete;
        /// <summary>Event raised when a plan fails during execution.</summary>
        public event Action<Plan, string> OnPlanFailed;
        /// <summary>Event raised when an action completes.</summary>
        public event Action<GOAPAction> OnActionComplete;

        /// <summary>Begins executing a plan.</summary>
        public void ExecutePlan(Plan plan)
        {
            if (_isExecuting) CancelPlan();

            _currentPlan = plan;
            _currentActionIndex = 0;
            _isExecuting = true;
            StartNextAction();
        }

        /// <summary>Updates the current action. Call this every frame.</summary>
        public void Update(float deltaTime, WorldState worldState)
        {
            if (!_isExecuting || _activeAction == null) return;

            _actionTimer += deltaTime;

            bool completed = _activeAction.OnUpdate(deltaTime);
            if (completed)
            {
                _activeAction.OnComplete();
                OnActionComplete?.Invoke(_activeAction);
                _currentActionIndex++;

                if (_currentActionIndex >= _currentPlan.Actions.Count)
                {
                    _isExecuting = false;
                    OnPlanComplete?.Invoke(_currentPlan);
                }
                else
                {
                    StartNextAction();
                }
            }
        }

        /// <summary>Cancels the current plan.</summary>
        public void CancelPlan()
        {
            if (_activeAction != null)
            {
                _activeAction.OnCancel();
                _activeAction = null;
            }
            _isExecuting = false;
        }

        private void StartNextAction()
        {
            if (_currentActionIndex >= _currentPlan.Actions.Count) return;
            _activeAction = _currentPlan.Actions[_currentActionIndex];
            _actionTimer = 0;
            _activeAction.OnStart();
        }
    }

    // =========================================================================
    // Concrete GOAP Actions for Shadow Protocol AI
    // =========================================================================

    /// <summary>Action: Move to a patrol waypoint.</summary>
    public class PatrolAction : GOAPAction
    {
        public override string Name => "Patrol";
        public override float Cost => 1f;
        public override float Duration => 5f;
        public override Dictionary<string, object> GetPreconditions() =>
            new() { ["isAlive"] = true, ["hasWeapon"] = true };
        public override Dictionary<string, object> GetEffects() =>
            new() { ["atPatrolPoint"] = true, ["areaScanned"] = true };
    }

    /// <summary>Action: Investigate a suspicious sound or sighting.</summary>
    public class InvestigateAction : GOAPAction
    {
        public override string Name => "Investigate";
        public override float Cost => 2f;
        public override float Duration => 8f;
        public override bool RequiresTarget => true;
        public override Dictionary<string, object> GetPreconditions() =>
            new() { ["isAlive"] = true, ["heardNoise"] = true };
        public override Dictionary<string, object> GetEffects() =>
            new() { ["areaInvestigated"] = true, ["heardNoise"] = false };
    }

    /// <summary>Action: Take cover behind nearby objects.</summary>
    public class TakeCoverAction : GOAPAction
    {
        public override string Name => "TakeCover";
        public override float Cost => 2f;
        public override float Duration => 2f;
        public override Dictionary<string, object> GetPreconditions() =>
            new() { ["isAlive"] = true, ["enemyVisible"] = true };
        public override Dictionary<string, object> GetEffects() =>
            new() { ["inCover"] = true, ["isExposed"] = false };
    }

    /// <summary>Action: Engage the enemy target with equipped weapon.</summary>
    public class EngageEnemyAction : GOAPAction
    {
        public override string Name => "EngageEnemy";
        public override float Cost => 3f;
        public override float Duration => 10f;
        public override bool RequiresTarget => true;
        public override Dictionary<string, object> GetPreconditions() =>
            new() { ["isAlive"] = true, ["hasWeapon"] = true, ["hasAmmo"] = true, ["enemyVisible"] = true };
        public override Dictionary<string, object> GetEffects() =>
            new() { ["enemyNeutralized"] = true };
    }

    /// <summary>Action: Reload the current weapon.</summary>
    public class ReloadAction : GOAPAction
    {
        public override string Name => "Reload";
        public override float Cost => 1.5f;
        public override float Duration => 3f;
        public override bool Interruptible => false;
        public override Dictionary<string, object> GetPreconditions() =>
            new() { ["isAlive"] = true, ["hasWeapon"] = true, ["hasAmmo"] = false };
        public override Dictionary<string, object> GetEffects() =>
            new() { ["hasAmmo"] = true };
    }

    /// <summary>Action: Use a health kit to heal injuries.</summary>
    public class HealAction : GOAPAction
    {
        public override string Name => "Heal";
        public override float Cost => 2f;
        public override float Duration => 4f;
        public override Dictionary<string, object> GetPreconditions() =>
            new() { ["isAlive"] = true, ["isInjured"] = true, ["hasMedkit"] = true };
        public override Dictionary<string, object> GetEffects() =>
            new() { ["isInjured"] = false, ["hasMedkit"] = false };
    }

    /// <summary>Action: Call for reinforcements from nearby squads.</summary>
    public class CallReinforcementsAction : GOAPAction
    {
        public override string Name => "CallReinforcements";
        public override float Cost => 4f;
        public override float Duration => 2f;
        public override Dictionary<string, object> GetPreconditions() =>
            new() { ["isAlive"] = true, ["hasRadio"] = true, ["enemyVisible"] = true };
        public override Dictionary<string, object> GetEffects() =>
            new() { ["reinforcementsCalled"] = true };
    }

    /// <summary>Action: Retreat to a safe position.</summary>
    public class RetreatAction : GOAPAction
    {
        public override string Name => "Retreat";
        public override float Cost => 5f;
        public override float Duration => 6f;
        public override Dictionary<string, object> GetPreconditions() =>
            new() { ["isAlive"] = true };
        public override Dictionary<string, object> GetEffects() =>
            new() { ["isExposed"] = false, ["inSafeZone"] = true, ["enemyVisible"] = false };
    }

    /// <summary>Action: Flank the enemy by moving to their side or rear.</summary>
    public class FlankAction : GOAPAction
    {
        public override string Name => "Flank";
        public override float Cost => 4f;
        public override float Duration => 8f;
        public override bool RequiresTarget => true;
        public override Dictionary<string, object> GetPreconditions() =>
            new() { ["isAlive"] = true, ["hasWeapon"] = true, ["enemyVisible"] = true };
        public override Dictionary<string, object> GetEffects() =>
            new() { ["hasFlankAdvantage"] = true, ["enemyNeutralized"] = true };
    }

    /// <summary>Action: Throw a grenade at the enemy position.</summary>
    public class ThrowGrenadeAction : GOAPAction
    {
        public override string Name => "ThrowGrenade";
        public override float Cost => 3f;
        public override float Duration => 2f;
        public override bool RequiresTarget => true;
        public override Dictionary<string, object> GetPreconditions() =>
            new() { ["isAlive"] = true, ["hasGrenade"] = true, ["enemyVisible"] = true };
        public override Dictionary<string, object> GetEffects() =>
            new() { ["hasGrenade"] = false, ["enemySuppressed"] = true };
    }

    /// <summary>Action: Loot supplies from nearby containers or fallen enemies.</summary>
    public class LootAction : GOAPAction
    {
        public override string Name => "Loot";
        public override float Cost => 2f;
        public override float Duration => 5f;
        public override bool RequiresTarget => true;
        public override Dictionary<string, object> GetPreconditions() =>
            new() { ["isAlive"] = true, ["nearLootSource"] = true };
        public override Dictionary<string, object> GetEffects() =>
            new() { ["hasAmmo"] = true, ["hasMedkit"] = true, ["nearLootSource"] = false };
    }
}
