using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Executes a SkillGraph at runtime
/// Handles execution flow, data passing, and node execution
/// </summary>
public class SkillGraphExecutor : MonoBehaviour
{
    private SkillGraph graph;
    private IActor performer;
    private Dictionary<string, object> dataValues = new Dictionary<string, object>();
    private Dictionary<string, bool> executionStates = new Dictionary<string, bool>();
    private bool isExecuting = false;
    
    // Store state for conditional execution nodes
    private Dictionary<string, bool> branchConditions = new Dictionary<string, bool>();
    private Dictionary<string, int> loopIterations = new Dictionary<string, int>();
    
    // Track nodes being executed to prevent infinite recursion in data resolution
    private HashSet<string> executingDataNodes = new HashSet<string>();
    
    /// <summary>
    /// Executes a skill graph with the given performer
    /// </summary>
    public static void Execute(SkillGraph graph, IActor performer)
    {
        if (graph == null || performer == null)
        {
            Debug.LogWarning("Cannot execute skill graph: graph or performer is null");
            return;
        }
        
        // Create a temporary executor to run the graph
        GameObject executorObj = new GameObject("SkillGraphExecutor");
        executorObj.hideFlags = HideFlags.HideAndDontSave;
        var executor = executorObj.AddComponent<SkillGraphExecutor>();
        executor.StartCoroutine(executor.ExecuteGraph(graph, performer));
    }
    
    /// <summary>
    /// Coroutine that executes the graph
    /// </summary>
    private IEnumerator ExecuteGraph(SkillGraph graph, IActor performer)
    {
        this.graph = graph;
        this.performer = performer;
        isExecuting = true;
        dataValues.Clear();
        executionStates.Clear();
        branchConditions.Clear();
        loopIterations.Clear();
        executingDataNodes.Clear();
        
        // Rebuild connection caches for all nodes
        foreach (var node in graph.Nodes)
        {
            node.RebuildCacheIfNeeded();
        }
        
        // Get entry node
        var entryNode = graph.GetEntryNode();
        if (entryNode == null)
        {
            Debug.LogError("Skill graph has no entry node!");
            Destroy(gameObject);
            yield break;
        }
        
        // Execute from entry node
        yield return StartCoroutine(ExecuteNode(entryNode));
        
        // Clean up
        isExecuting = false;
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Executes a node and continues execution flow
    /// </summary>
    private IEnumerator ExecuteNode(SkillGraphNode node)
    {
        if (node == null)
            yield break;
        
        // Execute node based on type (this sets up any necessary state)
        yield return StartCoroutine(ExecuteNodeLogic(node));
        
        // Handle special execution flow nodes
        if (node is BranchNode branchNode)
        {
            yield return StartCoroutine(ExecuteBranchNodeFlow(branchNode));
        }
        else if (node is LoopNode loopNode)
        {
            yield return StartCoroutine(ExecuteLoopNodeFlow(loopNode));
        }
        else if (node is SequenceNode sequenceNode)
        {
            yield return StartCoroutine(ExecuteSequenceNodeFlow(sequenceNode));
        }
        else if (node is ParallelNode parallelNode)
        {
            yield return StartCoroutine(ExecuteParallelNodeFlow(parallelNode));
        }
        else
        {
            // Standard execution: continue through all execution output pins
            var outputPins = node.GetOutputPins();
            var executionPins = outputPins.Where(p => p.Type == PinType.Execution).ToList();
            
            foreach (var execPin in executionPins)
            {
                var connections = node.GetConnections(execPin.Id);
                foreach (var connection in connections)
                {
                    var targetNode = graph.GetNode(connection.NodeId);
                    if (targetNode != null)
                    {
                        yield return StartCoroutine(ExecuteNode(targetNode));
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Executes the logic of a specific node
    /// </summary>
    private IEnumerator ExecuteNodeLogic(SkillGraphNode node)
    {
        // Get input data values for this node
        var inputData = GetInputDataForNode(node);
        
        // Execute based on node type
        switch (node)
        {
            case EntryNode entryNode:
                // Entry node does nothing, just passes execution
                break;
                
            case SequenceNode sequenceNode:
                // Sequence node executes its outputs in order
                yield return StartCoroutine(ExecuteSequenceNode(sequenceNode));
                break;
                
            case BranchNode branchNode:
                yield return StartCoroutine(ExecuteBranchNode(branchNode, inputData));
                break;
                
            case LoopNode loopNode:
                yield return StartCoroutine(ExecuteLoopNode(loopNode, inputData));
                break;
                
            case DelayNode delayNode:
                yield return StartCoroutine(ExecuteDelayNode(delayNode, inputData));
                break;
                
            case ParallelNode parallelNode:
                yield return StartCoroutine(ExecuteParallelNode(parallelNode));
                break;
                
            case HealNode healNode:
                yield return StartCoroutine(ExecuteHealNode(healNode, inputData));
                break;
                
            case DamageNode damageNode:
                yield return StartCoroutine(ExecuteDamageNode(damageNode, inputData));
                break;
                
            case ApplyStatusNode statusNode:
                yield return StartCoroutine(ExecuteApplyStatusNode(statusNode, inputData));
                break;
                
            case MoveNode moveNode:
                yield return StartCoroutine(ExecuteMoveNode(moveNode, inputData));
                break;
                
            case SpawnNode spawnNode:
                yield return StartCoroutine(ExecuteSpawnNode(spawnNode, inputData));
                break;
                
            case TargetSelfNode targetSelfNode:
                ExecuteTargetSelfNode(targetSelfNode);
                break;
                
            case TargetEnemyNode targetEnemyNode:
                ExecuteTargetEnemyNode(targetEnemyNode, inputData);
                break;
                
            case TargetAllyNode targetAllyNode:
                ExecuteTargetAllyNode(targetAllyNode, inputData);
                break;
                
            case TargetAreaNode targetAreaNode:
                ExecuteTargetAreaNode(targetAreaNode, inputData);
                break;
                
            case TargetRaycastNode raycastNode:
                ExecuteTargetRaycastNode(raycastNode, inputData);
                break;
                
            case FilterTargetsNode filterNode:
                ExecuteFilterTargetsNode(filterNode, inputData);
                break;
                
            case PlayAnimationNode animNode:
                yield return StartCoroutine(ExecutePlayAnimationNode(animNode, inputData));
                break;
                
            case SpawnParticleNode particleNode:
                yield return StartCoroutine(ExecuteSpawnParticleNode(particleNode, inputData));
                break;
                
            case PlaySoundNode soundNode:
                ExecutePlaySoundNode(soundNode, inputData);
                break;
                
            case CameraShakeNode shakeNode:
                ExecuteCameraShakeNode(shakeNode, inputData);
                break;
                
            case ConstantNode constantNode:
                ExecuteConstantNode(constantNode);
                break;
                
            case GetHealthNode healthNode:
                ExecuteGetHealthNode(healthNode, inputData);
                break;
                
            case GetPositionNode positionNode:
                ExecuteGetPositionNode(positionNode, inputData);
                break;
                
            case GetStatNode statNode:
                ExecuteGetStatNode(statNode, inputData);
                break;
                
            case CalculateNode calcNode:
                ExecuteCalculateNode(calcNode, inputData);
                break;
                
            case CompareNode compareNode:
                ExecuteCompareNode(compareNode, inputData);
                break;
        }
    }
    
    /// <summary>
    /// Gets input data values for a node by following connections from other nodes
    /// Executes data-providing nodes if they haven't been executed yet
    /// </summary>
    private Dictionary<string, object> GetInputDataForNode(SkillGraphNode node)
    {
        var inputData = new Dictionary<string, object>();
        var inputPins = node.GetInputPins();
        
        foreach (var inputPin in inputPins)
        {
            if (inputPin.Type == PinType.Data)
            {
                // Find nodes that connect to this input pin
                foreach (var otherNode in graph.Nodes)
                {
                    var connections = otherNode.GetAllConnections();
                    foreach (var kvp in connections)
                    {
                        foreach (var conn in kvp.Value)
                        {
                            if (conn.NodeId == node.Id && conn.PinId == inputPin.Id)
                            {
                                // Get the output value from the source node
                                var outputPin = NodePinHelper.GetPinById(otherNode.GetOutputPins(), kvp.Key);
                                if (outputPin != null)
                                {
                                    var key = $"{otherNode.Id}_{kvp.Key}";
                                    
                                    // If data value doesn't exist, execute the data-providing node
                                    // This handles data nodes that aren't in the execution flow
                                    if (!dataValues.ContainsKey(key))
                                    {
                                        // Check if this is a data-only node (no execution input pins)
                                        var sourceInputPins = otherNode.GetInputPins();
                                        var hasExecutionInput = sourceInputPins.Any(p => p.Type == PinType.Execution);
                                        
                                        // If it's a pure data node (like ConstantNode), execute it synchronously
                                        // Prevent infinite recursion by checking if we're already executing this node
                                        if (!hasExecutionInput && !executingDataNodes.Contains(otherNode.Id))
                                        {
                                            executingDataNodes.Add(otherNode.Id);
                                            ExecuteDataNodeSynchronously(otherNode);
                                            executingDataNodes.Remove(otherNode.Id);
                                        }
                                    }
                                    
                                    if (dataValues.ContainsKey(key))
                                    {
                                        inputData[inputPin.Id] = dataValues[key];
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        return inputData;
    }
    
    /// <summary>
    /// Executes a data-only node synchronously (for nodes without execution inputs)
    /// </summary>
    private void ExecuteDataNodeSynchronously(SkillGraphNode node)
    {
        // Get input data for this node (recursively resolve data dependencies)
        var inputData = GetInputDataForNode(node);
        
        // Execute based on node type - only handle data nodes that don't need coroutines
        switch (node)
        {
            case ConstantNode constantNode:
                ExecuteConstantNode(constantNode);
                break;
                
            case GetHealthNode healthNode:
                ExecuteGetHealthNode(healthNode, inputData);
                break;
                
            case GetPositionNode positionNode:
                ExecuteGetPositionNode(positionNode, inputData);
                break;
                
            case GetStatNode statNode:
                ExecuteGetStatNode(statNode, inputData);
                break;
                
            case CalculateNode calcNode:
                ExecuteCalculateNode(calcNode, inputData);
                break;
                
            case CompareNode compareNode:
                ExecuteCompareNode(compareNode, inputData);
                break;
                
            case TargetSelfNode targetSelfNode:
                ExecuteTargetSelfNode(targetSelfNode);
                break;
        }
    }
    
    /// <summary>
    /// Sets output data value for a node
    /// </summary>
    private void SetOutputData(SkillGraphNode node, string pinId, object value)
    {
        var key = $"{node.Id}_{pinId}";
        dataValues[key] = value;
    }
    
    // Flow Control Node Executors
    private IEnumerator ExecuteSequenceNode(SequenceNode node)
    {
        // Sequence node logic is handled in ExecuteSequenceNodeFlow
        yield break;
    }
    
    private IEnumerator ExecuteSequenceNodeFlow(SequenceNode node)
    {
        // Execute sequence outputs in order
        var outputPins = node.GetOutputPins();
        var executionPins = outputPins.Where(p => p.Type == PinType.Execution)
            .OrderBy(p => p.Id) // Order by pin ID to ensure consistent ordering
            .ToList();
        
        foreach (var execPin in executionPins)
        {
            var connections = node.GetConnections(execPin.Id);
            foreach (var connection in connections)
            {
                var targetNode = graph.GetNode(connection.NodeId);
                if (targetNode != null)
                {
                    yield return StartCoroutine(ExecuteNode(targetNode));
                }
            }
        }
    }
    
    private IEnumerator ExecuteBranchNode(BranchNode node, Dictionary<string, object> inputData)
    {
        bool condition = false;
        if (inputData.ContainsKey("condition_in"))
        {
            condition = (bool)inputData["condition_in"];
        }
        
        // Store condition result for use in ExecuteBranchNodeFlow
        branchConditions[node.Id] = condition;
        yield break;
    }
    
    private IEnumerator ExecuteBranchNodeFlow(BranchNode node)
    {
        // Get the condition result
        bool condition = branchConditions.ContainsKey(node.Id) ? branchConditions[node.Id] : false;
        
        // Determine which execution pin to follow
        string execPinId = condition ? "exec_out_true" : "exec_out_false";
        
        var connections = node.GetConnections(execPinId);
        foreach (var connection in connections)
        {
            var targetNode = graph.GetNode(connection.NodeId);
            if (targetNode != null)
            {
                yield return StartCoroutine(ExecuteNode(targetNode));
            }
        }
        
        // Clean up
        branchConditions.Remove(node.Id);
    }
    
    private IEnumerator ExecuteLoopNode(LoopNode node, Dictionary<string, object> inputData)
    {
        int count = node.LoopCount;
        if (node.UseInputCount && inputData.ContainsKey("count_in"))
        {
            count = (int)inputData["count_in"];
        }
        
        // Store loop count for use in ExecuteLoopNodeFlow
        loopIterations[node.Id] = count;
        yield break;
    }
    
    private IEnumerator ExecuteLoopNodeFlow(LoopNode node)
    {
        int count = loopIterations.ContainsKey(node.Id) ? loopIterations[node.Id] : node.LoopCount;
        
        // Get the loop execution pin
        string loopExecPinId = "exec_out_loop";
        var loopConnections = node.GetConnections(loopExecPinId);
        
        // Execute loop body for each iteration
        for (int i = 0; i < count; i++)
        {
            // Set index output for this iteration
            SetOutputData(node, "index_out", i);
            
            // Execute all connections from the loop pin
            foreach (var connection in loopConnections)
            {
                var targetNode = graph.GetNode(connection.NodeId);
                if (targetNode != null)
                {
                    yield return StartCoroutine(ExecuteNode(targetNode));
                }
            }
        }
        
        // After loop completes, execute the complete output
        string completeExecPinId = "exec_out_complete";
        var completeConnections = node.GetConnections(completeExecPinId);
        foreach (var connection in completeConnections)
        {
            var targetNode = graph.GetNode(connection.NodeId);
            if (targetNode != null)
            {
                yield return StartCoroutine(ExecuteNode(targetNode));
            }
        }
        
        // Clean up
        loopIterations.Remove(node.Id);
    }
    
    private IEnumerator ExecuteDelayNode(DelayNode node, Dictionary<string, object> inputData)
    {
        float duration = node.DelayDuration;
        if (node.UseInputDuration && inputData.ContainsKey("duration_in"))
        {
            duration = (float)inputData["duration_in"];
        }
        
        yield return new WaitForSeconds(duration);
    }
    
    private IEnumerator ExecuteParallelNode(ParallelNode node)
    {
        // Parallel node logic is handled in ExecuteParallelNodeFlow
        yield break;
    }
    
    private IEnumerator ExecuteParallelNodeFlow(ParallelNode node)
    {
        // Get all branch execution pins (excluding the "Complete" pin)
        var outputPins = node.GetOutputPins();
        var branchPins = outputPins.Where(p => p.Type == PinType.Execution && p.Id != "exec_out")
            .ToList();
        
        // Collect all branch execution tasks
        List<IEnumerator> branchTasks = new List<IEnumerator>();
        
        foreach (var branchPin in branchPins)
        {
            var connections = node.GetConnections(branchPin.Id);
            foreach (var connection in connections)
            {
                var targetNode = graph.GetNode(connection.NodeId);
                if (targetNode != null)
                {
                    // Collect the execution enumerator for each branch
                    branchTasks.Add(ExecuteNode(targetNode));
                }
            }
        }
        
        // Execute all branches in parallel by running them concurrently
        // We need to manually step through each coroutine
        if (branchTasks.Count > 0)
        {
            // Use a simple approach: run all branches by stepping through them
            // This is not true parallelism but simulates it by interleaving execution
            bool allComplete = false;
            while (!allComplete)
            {
                allComplete = true;
                foreach (var task in branchTasks)
                {
                    if (task != null && task.MoveNext())
                    {
                        allComplete = false;
                        // If the current step yields something, yield it
                        if (task.Current != null)
                        {
                            yield return task.Current;
                        }
                    }
                }
                
                if (!allComplete)
                {
                    yield return null; // Wait one frame before continuing
                }
            }
        }
        
        // After all branches complete, execute the complete output
        var completeConnections = node.GetConnections("exec_out");
        foreach (var connection in completeConnections)
        {
            var targetNode = graph.GetNode(connection.NodeId);
            if (targetNode != null)
            {
                yield return StartCoroutine(ExecuteNode(targetNode));
            }
        }
    }
    
    // Action Node Executors
    private IEnumerator ExecuteHealNode(HealNode node, Dictionary<string, object> inputData)
    {
        IActor target = null;
        if (inputData.ContainsKey("target_in"))
        {
            target = (IActor)inputData["target_in"];
        }
        
        if (target != null)
        {
            float amount = node.HealAmount;
            if (node.UseInputAmount && inputData.ContainsKey("amount_in"))
            {
                amount = (float)inputData["amount_in"];
            }
            
            // Apply healing (this would call actual healing logic)
            Debug.Log($"{node.Title}: Healing {target.Name} for {amount} HP");
            // TODO: Implement actual healing logic
        }
        
        yield break;
    }
    
    private IEnumerator ExecuteDamageNode(DamageNode node, Dictionary<string, object> inputData)
    {
        IActor target = null;
        if (inputData.ContainsKey("target_in"))
        {
            target = (IActor)inputData["target_in"];
        }
        
        if (target != null)
        {
            float amount = node.DamageAmount;
            if (node.UseInputAmount && inputData.ContainsKey("amount_in"))
            {
                amount = (float)inputData["amount_in"];
            }
            
            // Apply damage (this would call actual damage logic)
            Debug.Log($"{node.Title}: Damaging {target.Name} for {amount} damage");
            // TODO: Implement actual damage logic
        }
        
        yield break;
    }
    
    private IEnumerator ExecuteApplyStatusNode(ApplyStatusNode node, Dictionary<string, object> inputData)
    {
        IActor target = null;
        if (inputData.ContainsKey("target_in"))
        {
            target = (IActor)inputData["target_in"];
        }
        
        if (target != null)
        {
            float duration = node.Duration;
            if (node.UseInputDuration && inputData.ContainsKey("duration_in"))
            {
                duration = (float)inputData["duration_in"];
            }
            
            // Apply status (this would call actual status logic)
            Debug.Log($"{node.Title}: Applying {node.StatusName} to {target.Name} for {duration}s");
            // TODO: Implement actual status effect logic
        }
        
        yield break;
    }
    
    private IEnumerator ExecuteMoveNode(MoveNode node, Dictionary<string, object> inputData)
    {
        IActor actor = null;
        if (inputData.ContainsKey("actor_in"))
        {
            actor = (IActor)inputData["actor_in"];
        }
        
        if (actor != null && actor.Transform != null)
        {
            Vector3 targetPos = Vector3.zero;
            if (node.UseInputPosition && inputData.ContainsKey("target_position_in"))
            {
                targetPos = (Vector3)inputData["target_position_in"];
            }
            
            float speed = node.MoveSpeed;
            if (node.UseInputSpeed && inputData.ContainsKey("speed_in"))
            {
                speed = (float)inputData["speed_in"];
            }
            
            // Move actor (this would call actual movement logic)
            Debug.Log($"{node.Title}: Moving {actor.Name} to {targetPos} at speed {speed}");
            // TODO: Implement actual movement logic
        }
        
        yield break;
    }
    
    private IEnumerator ExecuteSpawnNode(SpawnNode node, Dictionary<string, object> inputData)
    {
        Vector3 position = node.DefaultPosition;
        if (node.UseInputPosition && inputData.ContainsKey("position_in"))
        {
            position = (Vector3)inputData["position_in"];
        }
        
        if (node.Prefab != null)
        {
            GameObject spawned = Instantiate(node.Prefab, position, node.DefaultRotation);
            SetOutputData(node, "spawned_out", spawned);
            Debug.Log($"{node.Title}: Spawned {node.Prefab.name} at {position}");
        }
        
        yield break;
    }
    
    // Target Node Executors
    private void ExecuteTargetSelfNode(TargetSelfNode node)
    {
        SetOutputData(node, "actor_out", performer);
    }
    
    private void ExecuteTargetEnemyNode(TargetEnemyNode node, Dictionary<string, object> inputData)
    {
        // TODO: Implement enemy finding logic
        List<IActor> enemies = new List<IActor>();
        SetOutputData(node, "enemies_out", enemies);
        if (enemies.Count > 0)
        {
            SetOutputData(node, "first_enemy_out", enemies[0]);
        }
    }
    
    private void ExecuteTargetAllyNode(TargetAllyNode node, Dictionary<string, object> inputData)
    {
        // TODO: Implement ally finding logic
        List<IActor> allies = new List<IActor>();
        SetOutputData(node, "allies_out", allies);
        if (allies.Count > 0)
        {
            SetOutputData(node, "first_ally_out", allies[0]);
        }
    }
    
    private void ExecuteTargetAreaNode(TargetAreaNode node, Dictionary<string, object> inputData)
    {
        // TODO: Implement area targeting logic
        List<IActor> actors = new List<IActor>();
        SetOutputData(node, "actors_out", actors);
    }
    
    private void ExecuteTargetRaycastNode(TargetRaycastNode node, Dictionary<string, object> inputData)
    {
        // TODO: Implement raycast logic
        bool hit = false;
        IActor hitActor = null;
        SetOutputData(node, "hit_out", hit);
        SetOutputData(node, "actor_out", hitActor);
    }
    
    private void ExecuteFilterTargetsNode(FilterTargetsNode node, Dictionary<string, object> inputData)
    {
        // TODO: Implement filtering logic
        List<IActor> actors = new List<IActor>();
        if (inputData.ContainsKey("actors_in"))
        {
            actors = (List<IActor>)inputData["actors_in"];
        }
        
        List<IActor> filtered = new List<IActor>(actors);
        SetOutputData(node, "filtered_out", filtered);
    }
    
    // Visual Node Executors
    private IEnumerator ExecutePlayAnimationNode(PlayAnimationNode node, Dictionary<string, object> inputData)
    {
        IActor actor = null;
        if (inputData.ContainsKey("actor_in"))
        {
            actor = (IActor)inputData["actor_in"];
        }
        
        if (actor != null)
        {
            string animName = node.AnimationName;
            if (node.UseInputAnimation && inputData.ContainsKey("animation_in"))
            {
                animName = (string)inputData["animation_in"];
            }
            
            // TODO: Implement animation playing logic
            Debug.Log($"{node.Title}: Playing animation {animName} on {actor.Name}");
        }
        
        yield break;
    }
    
    private IEnumerator ExecuteSpawnParticleNode(SpawnParticleNode node, Dictionary<string, object> inputData)
    {
        Vector3 position = node.DefaultPosition;
        if (node.UseInputPosition && inputData.ContainsKey("position_in"))
        {
            position = (Vector3)inputData["position_in"];
        }
        
        if (node.ParticlePrefab != null)
        {
            GameObject particle = Instantiate(node.ParticlePrefab, position, Quaternion.identity);
            if (node.AutoDestroy)
            {
                Destroy(particle, node.Duration);
            }
            Debug.Log($"{node.Title}: Spawned particle at {position}");
        }
        
        yield break;
    }
    
    private void ExecutePlaySoundNode(PlaySoundNode node, Dictionary<string, object> inputData)
    {
        Vector3 position = node.DefaultPosition;
        if (node.UseInputPosition && inputData.ContainsKey("position_in"))
        {
            position = (Vector3)inputData["position_in"];
        }
        
        if (node.AudioClip != null)
        {
            // TODO: Implement audio playing logic
            Debug.Log($"{node.Title}: Playing sound {node.AudioClip.name} at {position}");
        }
    }
    
    private void ExecuteCameraShakeNode(CameraShakeNode node, Dictionary<string, object> inputData)
    {
        float intensity = node.Intensity;
        if (node.UseInputIntensity && inputData.ContainsKey("intensity_in"))
        {
            intensity = (float)inputData["intensity_in"];
        }
        
        float duration = node.Duration;
        if (node.UseInputDuration && inputData.ContainsKey("duration_in"))
        {
            duration = (float)inputData["duration_in"];
        }
        
        // TODO: Implement camera shake logic
        Debug.Log($"{node.Title}: Shaking camera with intensity {intensity} for {duration}s");
    }
    
    // Data Node Executors
    private void ExecuteConstantNode(ConstantNode node)
    {
        object value = null;
        switch (node.ValueType)
        {
            case ConstantValueType.Int:
                value = node.IntValue;
                break;
            case ConstantValueType.Float:
                value = node.FloatValue;
                break;
            case ConstantValueType.Bool:
                value = node.BoolValue;
                break;
            case ConstantValueType.String:
                value = node.StringValue;
                break;
            case ConstantValueType.Vector3:
                value = node.Vector3Value;
                break;
        }
        
        SetOutputData(node, "value_out", value);
    }
    
    private void ExecuteGetHealthNode(GetHealthNode node, Dictionary<string, object> inputData)
    {
        IActor actor = null;
        if (inputData.ContainsKey("actor_in"))
        {
            actor = (IActor)inputData["actor_in"];
        }
        
        if (actor != null)
        {
            // TODO: Implement health retrieval logic
            float health = 100f; // Placeholder
            SetOutputData(node, "health_out", health);
        }
    }
    
    private void ExecuteGetPositionNode(GetPositionNode node, Dictionary<string, object> inputData)
    {
        Vector3 position = Vector3.zero;
        
        if (node.UseTransformInput && inputData.ContainsKey("transform_in"))
        {
            Transform transform = (Transform)inputData["transform_in"];
            if (transform != null)
            {
                position = transform.position;
            }
        }
        else if (inputData.ContainsKey("actor_in"))
        {
            IActor actor = (IActor)inputData["actor_in"];
            if (actor != null && actor.Transform != null)
            {
                position = actor.Transform.position;
            }
        }
        
        SetOutputData(node, "position_out", position);
    }
    
    private void ExecuteGetStatNode(GetStatNode node, Dictionary<string, object> inputData)
    {
        IActor actor = null;
        if (inputData.ContainsKey("actor_in"))
        {
            actor = (IActor)inputData["actor_in"];
        }
        
        if (actor != null)
        {
            // TODO: Implement stat retrieval logic
            float statValue = 10f; // Placeholder
            SetOutputData(node, "stat_out", statValue);
        }
    }
    
    private void ExecuteCalculateNode(CalculateNode node, Dictionary<string, object> inputData)
    {
        float a = 0f;
        float b = 0f;
        
        if (inputData.ContainsKey("a_in"))
            a = (float)inputData["a_in"];
        if (inputData.ContainsKey("b_in"))
            b = (float)inputData["b_in"];
        
        float result = 0f;
        switch (node.Operation)
        {
            case CalculateOperation.Add:
                result = a + b;
                break;
            case CalculateOperation.Subtract:
                result = a - b;
                break;
            case CalculateOperation.Multiply:
                result = a * b;
                break;
            case CalculateOperation.Divide:
                result = b != 0 ? a / b : 0f;
                break;
            case CalculateOperation.Modulo:
                result = b != 0 ? a % b : 0f;
                break;
            case CalculateOperation.Power:
                result = Mathf.Pow(a, b);
                break;
            case CalculateOperation.Min:
                result = Mathf.Min(a, b);
                break;
            case CalculateOperation.Max:
                result = Mathf.Max(a, b);
                break;
        }
        
        SetOutputData(node, "result_out", result);
    }
    
    private void ExecuteCompareNode(CompareNode node, Dictionary<string, object> inputData)
    {
        float a = 0f;
        float b = 0f;
        
        if (inputData.ContainsKey("a_in"))
            a = (float)inputData["a_in"];
        if (inputData.ContainsKey("b_in"))
            b = (float)inputData["b_in"];
        
        bool result = false;
        switch (node.Operation)
        {
            case CompareOperation.Equal:
                result = Mathf.Approximately(a, b);
                break;
            case CompareOperation.NotEqual:
                result = !Mathf.Approximately(a, b);
                break;
            case CompareOperation.GreaterThan:
                result = a > b;
                break;
            case CompareOperation.LessThan:
                result = a < b;
                break;
            case CompareOperation.GreaterThanOrEqual:
                result = a >= b;
                break;
            case CompareOperation.LessThanOrEqual:
                result = a <= b;
                break;
        }
        
        SetOutputData(node, "result_out", result);
    }
}

