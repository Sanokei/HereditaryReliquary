using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class SkillGraphEditor : EditorWindow
{
    public Skill selectedSkill;
    public SkillGraph skillGraph;
    private SkillGraphNode selectedNode;
    private SkillGraphNode connectingFromNode;
    private string connectingFromPinId;
    private Vector2 panOffset = Vector2.zero;
    private float zoom = 1f;
    private const float NODE_WIDTH = 200f;
    private const float NODE_HEIGHT = 120f;
    private const float CONNECTION_HANDLE_SIZE = 15f;
    private const float CONNECTION_LINE_WIDTH = 6f;
    private const float CONNECTION_ARROW_SIZE = 10f;
    
    private Vector2 lastMousePosition;
    private bool isDragging = false;
    private bool isPanning = false;
    private bool isConnecting = false;
    private bool showNodePalette = true;
    private Vector2 nodePaletteScroll;
    private Vector2 propertiesScroll;
    private string searchFilter = "";
    
    // Node creation
    private System.Type selectedNodeType;
    private Rect nodePaletteRect;
    
    [MenuItem("Tools/Skill Graph Editor")]
    public static void ShowWindow()
    {
        GetWindow<SkillGraphEditor>("Skill Graph Editor");
    }
    
    /// <summary>
    /// Opens the Skill Graph Editor with a specific skill
    /// </summary>
    public static void OpenWithSkill(Skill skill)
    {
        var window = GetWindow<SkillGraphEditor>("Skill Graph Editor");
        window.selectedSkill = skill;
        window.skillGraph = skill != null ? skill.GetSkillGraph() : null;
        window.Repaint();
    }
    
    private void OnEnable()
    {
        minSize = new Vector2(800, 600);
    }
    
    private void OnGUI()
    {
        DrawToolbar();
        
        if (selectedSkill == null)
        {
            DrawEmptyState();
            return;
        }
        
        // Update skillGraph from selectedSkill
        if (skillGraph != selectedSkill.GetSkillGraph())
        {
            skillGraph = selectedSkill.GetSkillGraph();
        }
        
        // If no skill graph, show message with button to create one
        if (skillGraph == null)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space(50);
            EditorGUILayout.LabelField($"No Skill Graph for {selectedSkill.SkillName}", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Create Skill Graph", GUILayout.Height(30)))
            {
                CreateSkillGraphForSkill();
            }
            EditorGUILayout.EndVertical();
            return;
        }
        
        // Split view: palette, graph
        EditorGUILayout.BeginHorizontal();
        
        // Node Palette
        if (showNodePalette)
        {
            DrawNodePalette();
        }
        
        // Main graph area
        EditorGUILayout.BeginVertical();
        HandleInput();
        
        // Draw background
        DrawGrid(20, 0.2f, Color.gray);
        DrawGrid(100, 0.4f, Color.gray);
        
        // Draw connections
        DrawConnections();
        
        // Draw nodes
        DrawNodes();
        
        // Draw connection preview
        if (isConnecting && connectingFromNode != null)
        {
            DrawConnectionPreview();
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndHorizontal();
        
        // Edit Skill Graph button at bottom
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Edit Skill Graph", GUILayout.Width(150), GUILayout.Height(30)))
        {
            // Focus is already on the graph, but we can ensure the window is focused
            Focus();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        if (GUI.changed)
        {
            if (skillGraph != null)
            {
                EditorUtility.SetDirty(skillGraph);
            }
            if (selectedSkill != null)
            {
                EditorUtility.SetDirty(selectedSkill);
            }
        }
    }
    
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        // Skill field
        EditorGUILayout.LabelField("Skill:", GUILayout.Width(50));
        Skill newSkill = (Skill)EditorGUILayout.ObjectField(selectedSkill, typeof(Skill), false, GUILayout.Width(200));
        if (newSkill != selectedSkill)
        {
            selectedSkill = newSkill;
            skillGraph = selectedSkill != null ? selectedSkill.GetSkillGraph() : null;
        }
        
        GUILayout.FlexibleSpace();
        
        if (selectedSkill != null && skillGraph != null)
        {
            // Toggle palette
            if (GUILayout.Button(showNodePalette ? "Hide Palette" : "Show Palette", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                showNodePalette = !showNodePalette;
            }
            
            // Validate button
            if (GUILayout.Button("Validate", EditorStyles.toolbarButton))
            {
                ValidateGraph();
            }
            
            // Delete selected node (but not entry node)
            if (selectedNode != null && !(selectedNode is EntryNode) && GUILayout.Button("Delete Node", EditorStyles.toolbarButton))
            {
                DeleteNode(selectedNode);
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawEmptyState()
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.Space(50);
        EditorGUILayout.LabelField("No Skill Selected", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(10);
        if (GUILayout.Button("Create New Skill", GUILayout.Height(30)))
        {
            CreateNewSkill();
        }
        EditorGUILayout.EndVertical();
    }
    
    private void CreateNewSkill()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create Skill", "NewSkill", "asset", "Create a new Skill");
        if (!string.IsNullOrEmpty(path))
        {
            // Create the Skill
            selectedSkill = CreateInstance<Skill>();
            
            // Set default skill name from filename
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            // Use reflection to set the private skillName field
            var skillNameField = typeof(Skill).GetField("skillName", BindingFlags.NonPublic | BindingFlags.Instance);
            if (skillNameField != null)
            {
                skillNameField.SetValue(selectedSkill, fileName);
            }
            
            // Create and assign the SkillGraph
            CreateSkillGraphForSkill();
            
            AssetDatabase.CreateAsset(selectedSkill, path);
            
            // Save the skill graph as a sub-asset
            if (skillGraph != null)
            {
                skillGraph.name = selectedSkill.SkillName + "_Graph";
                AssetDatabase.AddObjectToAsset(skillGraph, selectedSkill);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Select the created skill in the project window
            Selection.activeObject = selectedSkill;
        }
    }
    
    private void CreateSkillGraphForSkill()
    {
        if (selectedSkill == null) return;
        
        // Create the skill graph
        skillGraph = CreateInstance<SkillGraph>();
        skillGraph.Nodes.Clear();
        
        // Add entry node in the center
        Vector2 centerPos = new Vector2(position.width / 2 - NODE_WIDTH / 2, position.height / 2 - NODE_HEIGHT / 2);
        var entryNode = new EntryNode(centerPos);
        skillGraph.AddNode(entryNode);
        skillGraph.SetEntryNode(entryNode.Id);
        
        // Assign to skill
        selectedSkill.SetSkillGraph(skillGraph);
        
        // If the skill is already saved as an asset, add the graph as a sub-asset
        if (AssetDatabase.Contains(selectedSkill))
        {
            skillGraph.name = selectedSkill.SkillName + "_Graph";
            AssetDatabase.AddObjectToAsset(skillGraph, selectedSkill);
            AssetDatabase.SaveAssets();
        }
    }
    
    private void DrawNodePalette()
    {
        GUILayout.BeginVertical(GUILayout.Width(250));
        
        EditorGUILayout.LabelField("Node Palette", EditorStyles.boldLabel);
        
        // Search filter
        searchFilter = EditorGUILayout.TextField("Search:", searchFilter);
        
        nodePaletteScroll = EditorGUILayout.BeginScrollView(nodePaletteScroll);
        
        // Flow Control Nodes
        DrawPaletteCategory("Flow Control", new System.Type[]
        {
            typeof(EntryNode),
            typeof(SequenceNode),
            typeof(BranchNode),
            typeof(LoopNode),
            typeof(DelayNode),
            typeof(ParallelNode)
        });
        
        // Action Nodes
        DrawPaletteCategory("Actions", new System.Type[]
        {
            typeof(HealNode),
            typeof(DamageNode),
            typeof(ApplyStatusNode),
            typeof(MoveNode),
            typeof(SpawnNode)
        });
        
        // Target Nodes
        DrawPaletteCategory("Targets", new System.Type[]
        {
            typeof(TargetSelfNode),
            typeof(TargetEnemyNode),
            typeof(TargetAllyNode),
            typeof(TargetAreaNode),
            typeof(TargetRaycastNode),
            typeof(FilterTargetsNode)
        });
        
        // Visual Nodes
        DrawPaletteCategory("Visual/Audio", new System.Type[]
        {
            typeof(PlayAnimationNode),
            typeof(SpawnParticleNode),
            typeof(PlaySoundNode),
            typeof(CameraShakeNode)
        });
        
        // Data Nodes
        DrawPaletteCategory("Data", new System.Type[]
        {
            typeof(ConstantNode),
            typeof(GetHealthNode),
            typeof(GetPositionNode),
            typeof(GetStatNode),
            typeof(CalculateNode),
            typeof(CompareNode)
        });
        
        EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();
    }
    
    private void DrawPaletteCategory(string categoryName, System.Type[] nodeTypes)
    {
        EditorGUILayout.LabelField(categoryName, EditorStyles.boldLabel);
        
        foreach (var nodeType in nodeTypes)
        {
            string nodeName = nodeType.Name.Replace("Node", "");
            if (string.IsNullOrEmpty(searchFilter) || nodeName.ToLower().Contains(searchFilter.ToLower()))
            {
                if (GUILayout.Button(nodeName, EditorStyles.miniButton))
                {
                    CreateNode(nodeType);
                }
            }
        }
        
        EditorGUILayout.Space(5);
    }
    
    private void CreateNode(System.Type nodeType)
    {
        if (skillGraph == null) return;
        
        Vector2 newNodePos = Event.current.mousePosition - panOffset;
        SkillGraphNode newNode = null;
        
        // Create instance based on type
        if (nodeType == typeof(EntryNode))
            newNode = new EntryNode(newNodePos);
        else if (nodeType == typeof(SequenceNode))
            newNode = new SequenceNode(newNodePos);
        else if (nodeType == typeof(BranchNode))
            newNode = new BranchNode(newNodePos);
        else if (nodeType == typeof(LoopNode))
            newNode = new LoopNode(newNodePos);
        else if (nodeType == typeof(DelayNode))
            newNode = new DelayNode(newNodePos);
        else if (nodeType == typeof(ParallelNode))
            newNode = new ParallelNode(newNodePos);
        else if (nodeType == typeof(HealNode))
            newNode = new HealNode(newNodePos);
        else if (nodeType == typeof(DamageNode))
            newNode = new DamageNode(newNodePos);
        else if (nodeType == typeof(ApplyStatusNode))
            newNode = new ApplyStatusNode(newNodePos);
        else if (nodeType == typeof(MoveNode))
            newNode = new MoveNode(newNodePos);
        else if (nodeType == typeof(SpawnNode))
            newNode = new SpawnNode(newNodePos);
        else if (nodeType == typeof(TargetSelfNode))
            newNode = new TargetSelfNode(newNodePos);
        else if (nodeType == typeof(TargetEnemyNode))
            newNode = new TargetEnemyNode(newNodePos);
        else if (nodeType == typeof(TargetAllyNode))
            newNode = new TargetAllyNode(newNodePos);
        else if (nodeType == typeof(TargetAreaNode))
            newNode = new TargetAreaNode(newNodePos);
        else if (nodeType == typeof(TargetRaycastNode))
            newNode = new TargetRaycastNode(newNodePos);
        else if (nodeType == typeof(FilterTargetsNode))
            newNode = new FilterTargetsNode(newNodePos);
        else if (nodeType == typeof(PlayAnimationNode))
            newNode = new PlayAnimationNode(newNodePos);
        else if (nodeType == typeof(SpawnParticleNode))
            newNode = new SpawnParticleNode(newNodePos);
        else if (nodeType == typeof(PlaySoundNode))
            newNode = new PlaySoundNode(newNodePos);
        else if (nodeType == typeof(CameraShakeNode))
            newNode = new CameraShakeNode(newNodePos);
        else if (nodeType == typeof(ConstantNode))
            newNode = new ConstantNode(newNodePos);
        else if (nodeType == typeof(GetHealthNode))
            newNode = new GetHealthNode(newNodePos);
        else if (nodeType == typeof(GetPositionNode))
            newNode = new GetPositionNode(newNodePos);
        else if (nodeType == typeof(GetStatNode))
            newNode = new GetStatNode(newNodePos);
        else if (nodeType == typeof(CalculateNode))
            newNode = new CalculateNode(newNodePos);
        else if (nodeType == typeof(CompareNode))
            newNode = new CompareNode(newNodePos);
        
        if (newNode != null)
        {
            skillGraph.AddNode(newNode);
            selectedNode = newNode;
            
            // If it's an EntryNode and there's no entry node, set it
            if (newNode is EntryNode && string.IsNullOrEmpty(skillGraph.EntryNodeId))
            {
                skillGraph.SetEntryNode(newNode.Id);
            }
            
            GUI.changed = true;
            Repaint();
        }
    }
    
    // Removed DrawPropertiesPanel - properties are now inline in nodes
    // All old property drawer methods removed - using DrawNodePropertiesInNode instead
    
    // Drawing methods
    private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
    {
        int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
        int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);
        
        Handles.BeginGUI();
        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);
        
        Vector3 newOffset = new Vector3(panOffset.x % gridSpacing, panOffset.y % gridSpacing, 0);
        
        for (int i = 0; i < widthDivs; i++)
        {
            Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset,
                           new Vector3(gridSpacing * i, position.height, 0f) + newOffset);
        }
        
        for (int j = 0; j < heightDivs; j++)
        {
            Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset,
                           new Vector3(position.width, gridSpacing * j, 0f) + newOffset);
        }
        
        Handles.color = Color.white;
        Handles.EndGUI();
    }
    
    // Removed all old property drawer methods - they're now in DrawNodePropertiesInNode
    
    private void DrawNodes()
    {
        if (skillGraph == null || skillGraph.Nodes == null) return;
        
        foreach (var node in skillGraph.Nodes)
        {
            DrawNode(node);
        }
    }
    
    private void DrawNode(SkillGraphNode node)
    {
        Vector2 nodePos = node.Position + panOffset;
        Rect nodeRect = new Rect(nodePos.x, nodePos.y, NODE_WIDTH, NODE_HEIGHT);
        
        // Draw node background
        Color backgroundColor = (node == selectedNode) ? new Color(0.3f, 0.5f, 0.8f, 0.8f) : new Color(0.2f, 0.2f, 0.2f, 0.8f);
        EditorGUI.DrawRect(nodeRect, backgroundColor);
        
        // Draw node border
        Handles.BeginGUI();
        Handles.color = (node == selectedNode) ? Color.yellow : Color.white;
        Handles.DrawAAPolyLine(3f, new Vector3[] {
            new Vector3(nodeRect.x, nodeRect.y, 0),
            new Vector3(nodeRect.x + nodeRect.width, nodeRect.y, 0),
            new Vector3(nodeRect.x + nodeRect.width, nodeRect.y + nodeRect.height, 0),
            new Vector3(nodeRect.x, nodeRect.y + nodeRect.height, 0),
            new Vector3(nodeRect.x, nodeRect.y, 0)
        });
        Handles.EndGUI();
        
        // Draw node content with properties inside
        GUILayout.BeginArea(nodeRect);
        EditorGUILayout.BeginVertical();
        
        // Node title
        EditorGUILayout.LabelField(node.Title, EditorStyles.boldLabel);
        
        // Draw node properties inside the node
        DrawNodePropertiesInNode(node);
        
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
        
        // Draw connection handles on the sides
        Vector2 outputHandlePos = nodePos + new Vector2(NODE_WIDTH, NODE_HEIGHT / 2);
        Vector2 inputHandlePos = nodePos + new Vector2(0, NODE_HEIGHT / 2);
        
        Rect outputHandleRect = new Rect(outputHandlePos.x - CONNECTION_HANDLE_SIZE / 2, 
                                        outputHandlePos.y - CONNECTION_HANDLE_SIZE / 2, 
                                        CONNECTION_HANDLE_SIZE, CONNECTION_HANDLE_SIZE);
        Rect inputHandleRect = new Rect(inputHandlePos.x - CONNECTION_HANDLE_SIZE / 2, 
                                       inputHandlePos.y - CONNECTION_HANDLE_SIZE / 2, 
                                       CONNECTION_HANDLE_SIZE, CONNECTION_HANDLE_SIZE);
        
        // Draw output handle (right side) - green for execution, cyan for data
        Handles.BeginGUI();
        var outputPins = node.GetOutputPins();
        var execPins = outputPins.Where(p => p.Type == PinType.Execution).ToList();
        Handles.color = (connectingFromNode == node) ? Color.yellow : (execPins.Count > 0 ? Color.green : Color.cyan);
        Handles.DrawSolidDisc(outputHandlePos, Vector3.forward, CONNECTION_HANDLE_SIZE / 2);
        Handles.color = Color.white;
        Handles.EndGUI();
        
        // Draw input handle (left side) - red
        Handles.BeginGUI();
        Handles.color = Color.red;
        Handles.DrawSolidDisc(inputHandlePos, Vector3.forward, CONNECTION_HANDLE_SIZE / 2);
        Handles.color = Color.white;
        Handles.EndGUI();
        
        // Handle connection handle clicks
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            if (outputHandleRect.Contains(Event.current.mousePosition))
            {
                // Start connection from first execution output pin, or first output pin
                var execOutputPins = outputPins.Where(p => p.Type == PinType.Execution).ToList();
                if (execOutputPins.Count > 0)
                {
                    if (connectingFromNode == node)
                    {
                        connectingFromNode = null;
                        connectingFromPinId = null;
                        isConnecting = false;
                    }
                    else
                    {
                        connectingFromNode = node;
                        connectingFromPinId = execOutputPins[0].Id;
                        isConnecting = true;
                        // Deselect node when starting connection
                        selectedNode = null;
                    }
                    Event.current.Use();
                }
            }
            else if (inputHandleRect.Contains(Event.current.mousePosition) && connectingFromNode != null && connectingFromNode != node)
            {
                // Complete connection to first execution input pin
                var inputPins = node.GetInputPins();
                var execInputPins = inputPins.Where(p => p.Type == PinType.Execution).ToList();
                if (execInputPins.Count > 0)
                {
                    if (connectingFromNode.AddConnection(connectingFromPinId, skillGraph, node.Id, execInputPins[0].Id))
                    {
                        connectingFromNode = null;
                        connectingFromPinId = null;
                        isConnecting = false;
                        // Deselect node when creating connection
                        selectedNode = null;
                        GUI.changed = true;
                    }
                    else
                    {
                        Debug.LogWarning("Cannot create connection: Type mismatch or invalid connection");
                    }
                    Event.current.Use();
                }
            }
        }
        
        // Handle node selection and dragging (single click to select)
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && nodeRect.Contains(Event.current.mousePosition))
        {
            if (!outputHandleRect.Contains(Event.current.mousePosition) && !inputHandleRect.Contains(Event.current.mousePosition))
            {
                selectedNode = node;
                isDragging = true;
                lastMousePosition = Event.current.mousePosition;
                Event.current.Use();
            }
        }
        
        if (isDragging && selectedNode == node && !(node is EntryNode))
        {
            if (Event.current.type == EventType.MouseDrag)
            {
                Vector2 delta = Event.current.mousePosition - lastMousePosition;
                node.Position += delta;
                lastMousePosition = Event.current.mousePosition;
                GUI.changed = true;
                Event.current.Use();
            }
        }
        
        // Prevent dragging entry node - keep it locked in center
        if (node is EntryNode)
        {
            // Calculate center position relative to current view
            float centerX = (position.width - (showNodePalette ? 250 : 0)) / 2 - NODE_WIDTH / 2;
            float centerY = position.height / 2 - NODE_HEIGHT / 2;
            Vector2 centerPos = new Vector2(centerX - panOffset.x, centerY - panOffset.y);
            
            if (Vector2.Distance(node.Position, centerPos) > 1f)
            {
                node.Position = centerPos;
                GUI.changed = true;
            }
        }
    }
    
    // All old property drawer methods removed - they're now in DrawNodePropertiesInNode
    
    private void DrawConnections()
    {
        if (skillGraph == null || skillGraph.Nodes == null) return;
        
        Handles.BeginGUI();
        
        foreach (var node in skillGraph.Nodes)
        {
            var connections = node.GetAllConnections();
            foreach (var kvp in connections)
            {
                foreach (var connection in kvp.Value)
                {
                    var targetNode = skillGraph.GetNode(connection.NodeId);
                    if (targetNode == null) continue;
                    
                    DrawConnection(node, targetNode);
                }
            }
        }
        
        Handles.EndGUI();
    }
    
    private void DrawConnection(SkillGraphNode fromNode, SkillGraphNode toNode)
    {
        Vector2 fromPos = fromNode.Position + panOffset + new Vector2(NODE_WIDTH, NODE_HEIGHT / 2);
        Vector2 toPos = toNode.Position + panOffset + new Vector2(0, NODE_HEIGHT / 2);
        
        // Draw line
        Handles.color = Color.white;
        Handles.DrawAAPolyLine(CONNECTION_LINE_WIDTH, fromPos, toPos);
        
        // Draw arrow
        Vector2 direction = (toPos - fromPos).normalized;
        Vector2 arrowBase = toPos - direction * 20f;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x) * CONNECTION_ARROW_SIZE;
        
        Handles.DrawAAPolyLine(CONNECTION_LINE_WIDTH, arrowBase + perpendicular, toPos);
        Handles.DrawAAPolyLine(CONNECTION_LINE_WIDTH, arrowBase - perpendicular, toPos);
    }
    
    private void DrawConnectionPreview()
    {
        if (connectingFromNode == null) return;
        
        Vector2 fromPos = connectingFromNode.Position + panOffset + new Vector2(NODE_WIDTH, NODE_HEIGHT / 2);
        Vector2 toPos = Event.current.mousePosition;
        
        Handles.BeginGUI();
        Handles.color = Color.yellow;
        Handles.DrawAAPolyLine(CONNECTION_LINE_WIDTH, fromPos, toPos);
        
        // Draw preview arrow
        Vector2 direction = (toPos - fromPos).normalized;
        if (direction.magnitude > 0.1f)
        {
            Vector2 arrowBase = toPos - direction * 20f;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * CONNECTION_ARROW_SIZE;
            Handles.DrawAAPolyLine(CONNECTION_LINE_WIDTH, arrowBase + perpendicular, toPos);
            Handles.DrawAAPolyLine(CONNECTION_LINE_WIDTH, arrowBase - perpendicular, toPos);
        }
        
        Handles.EndGUI();
        Repaint();
    }
    
    // Removed all duplicate old property drawer methods - they're now in DrawNodePropertiesInNode below
    
    private void HandleInput()
    {
        Event e = Event.current;
        
        // Panning
        if (e.type == EventType.MouseDown && (e.button == 2 || (e.button == 0 && e.alt)))
        {
            isPanning = true;
            lastMousePosition = e.mousePosition;
            e.Use();
        }
        
        if (isPanning && e.type == EventType.MouseDrag)
        {
            Vector2 delta = e.mousePosition - lastMousePosition;
            panOffset += delta;
            lastMousePosition = e.mousePosition;
            e.Use();
            Repaint();
        }
        
        if (e.type == EventType.MouseUp && (e.button == 2 || e.button == 0))
        {
            isPanning = false;
            isDragging = false;
            e.Use();
        }
        
        // Zoom
        if (e.type == EventType.ScrollWheel)
        {
            zoom += e.delta.y * 0.01f;
            zoom = Mathf.Clamp(zoom, 0.5f, 2f);
            e.Use();
        }
        
        // Cancel connection on escape or right click
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            if (isConnecting)
            {
                isConnecting = false;
                connectingFromNode = null;
                connectingFromPinId = null;
            }
            // Also deselect nodes on escape
            else
            {
                selectedNode = null;
            }
            e.Use();
        }
        
        // Right-click to cancel connection
        if (e.type == EventType.MouseDown && e.button == 1 && isConnecting)
        {
            isConnecting = false;
            connectingFromNode = null;
            connectingFromPinId = null;
            e.Use();
        }
        
        // Delete node (but not entry node)
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete && selectedNode != null && !(selectedNode is EntryNode))
        {
            DeleteNode(selectedNode);
            e.Use();
        }
        
        // Deselect on background click (single click)
        if (e.type == EventType.MouseDown && e.button == 0 && !isConnecting && !isPanning)
        {
            // Check if clicking on background (not on any node or handle)
            bool clickedOnNode = false;
            if (skillGraph != null && skillGraph.Nodes != null)
            {
                foreach (var node in skillGraph.Nodes)
                {
                    Vector2 nodePos = node.Position + panOffset;
                    Rect nodeRect = new Rect(nodePos.x, nodePos.y, NODE_WIDTH, NODE_HEIGHT);
                    
                    if (nodeRect.Contains(e.mousePosition))
                    {
                        clickedOnNode = true;
                        break;
                    }
                    
                    // Check handles
                    Vector2 outputHandlePos = nodePos + new Vector2(NODE_WIDTH, NODE_HEIGHT / 2);
                    Vector2 inputHandlePos = nodePos + new Vector2(0, NODE_HEIGHT / 2);
                    Rect outputHandleRect = new Rect(outputHandlePos.x - CONNECTION_HANDLE_SIZE / 2, 
                                                    outputHandlePos.y - CONNECTION_HANDLE_SIZE / 2, 
                                                    CONNECTION_HANDLE_SIZE, CONNECTION_HANDLE_SIZE);
                    Rect inputHandleRect = new Rect(inputHandlePos.x - CONNECTION_HANDLE_SIZE / 2, 
                                                   inputHandlePos.y - CONNECTION_HANDLE_SIZE / 2, 
                                                   CONNECTION_HANDLE_SIZE, CONNECTION_HANDLE_SIZE);
                    if (outputHandleRect.Contains(e.mousePosition) || inputHandleRect.Contains(e.mousePosition))
                    {
                        clickedOnNode = true;
                        break;
                    }
                }
            }
            
            // If clicking on background, deselect everything (don't consume event so node selection can still work)
            if (!clickedOnNode)
            {
                selectedNode = null;
                GUI.changed = true;
                // Don't consume event - let other handlers process it if needed
            }
        }
    }
    
    private void DeleteNode(SkillGraphNode node)
    {
        if (skillGraph != null && node != null)
        {
            // Don't allow deleting entry node at all
            if (node is EntryNode)
            {
                Debug.LogWarning("Cannot delete the Entry node!");
                return;
            }
            
            skillGraph.RemoveNode(node.Id);
            if (selectedNode == node)
            {
                selectedNode = null;
            }
            if (connectingFromNode == node)
            {
                connectingFromNode = null;
                isConnecting = false;
            }
            GUI.changed = true;
        }
    }
    
    private void ValidateGraph()
    {
        if (skillGraph == null) return;
        
        List<string> errors = new List<string>();
        List<string> warnings = new List<string>();
        
        // Check for entry node
        if (string.IsNullOrEmpty(skillGraph.EntryNodeId))
        {
            errors.Add("Graph has no entry node!");
        }
        else
        {
            var entryNode = skillGraph.GetEntryNode();
            if (entryNode == null)
            {
                errors.Add("Entry node ID is invalid!");
            }
            else if (!(entryNode is EntryNode))
            {
                errors.Add("Entry node must be an EntryNode type!");
            }
        }
        
        // Check for orphaned nodes
        foreach (var node in skillGraph.Nodes)
        {
            if (node is EntryNode) continue;
            
            // Check if node has any execution input connections
            bool hasExecutionInput = false;
            foreach (var otherNode in skillGraph.Nodes)
            {
                var connections = otherNode.GetAllConnections();
                foreach (var kvp in connections)
                {
                    var fromPin = NodePinHelper.GetPinById(otherNode.GetOutputPins(), kvp.Key);
                    if (fromPin != null && fromPin.Type == PinType.Execution)
                    {
                        foreach (var conn in kvp.Value)
                        {
                            if (conn.NodeId == node.Id)
                            {
                                var toPin = NodePinHelper.GetPinById(node.GetInputPins(), conn.PinId);
                                if (toPin != null && toPin.Type == PinType.Execution)
                                {
                                    hasExecutionInput = true;
                                    break;
                                }
                            }
                        }
                        if (hasExecutionInput) break;
                    }
                }
                if (hasExecutionInput) break;
            }
            
            if (!hasExecutionInput)
            {
                warnings.Add($"Node '{node.Title}' has no execution input connection");
            }
        }
        
        // Display results
        if (errors.Count > 0 || warnings.Count > 0)
        {
            string message = "Graph Validation Results:\n\n";
            if (errors.Count > 0)
            {
                message += "Errors:\n";
                foreach (var error in errors)
                {
                    message += "• " + error + "\n";
                }
                message += "\n";
            }
            if (warnings.Count > 0)
            {
                message += "Warnings:\n";
                foreach (var warning in warnings)
                {
                    message += "• " + warning + "\n";
                }
            }
            
            EditorUtility.DisplayDialog("Graph Validation", message, "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Graph Validation", "Graph is valid! No errors or warnings found.", "OK");
        }
    }
    
    // All old property drawer methods and duplicate drawing methods removed - they're now in DrawNodePropertiesInNode below
    
    private void DrawNodePropertiesInNode(SkillGraphNode node)
    {
        // Draw properties inside the node itself
        switch (node)
        {
            case SequenceNode seqNode:
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Count:", GUILayout.Width(50));
                int count = EditorGUILayout.IntField(seqNode.SequenceCount, GUILayout.Width(50));
                if (count != seqNode.SequenceCount)
                {
                    seqNode.SequenceCount = count;
                    GUI.changed = true;
                }
                EditorGUILayout.EndHorizontal();
                break;
                
            case LoopNode loopNode:
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Count:", GUILayout.Width(50));
                int loopCount = EditorGUILayout.IntField(loopNode.LoopCount, GUILayout.Width(50));
                if (loopCount != loopNode.LoopCount)
                {
                    loopNode.LoopCount = loopCount;
                    GUI.changed = true;
                }
                EditorGUILayout.EndHorizontal();
                break;
                
            case DelayNode delayNode:
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Duration:", GUILayout.Width(60));
                float duration = EditorGUILayout.FloatField(delayNode.DelayDuration, GUILayout.Width(60));
                if (duration != delayNode.DelayDuration)
                {
                    delayNode.DelayDuration = duration;
                    GUI.changed = true;
                }
                EditorGUILayout.EndHorizontal();
                break;
                
            case HealNode healNode:
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Amount:", GUILayout.Width(60));
                float healAmount = EditorGUILayout.FloatField(healNode.HealAmount, GUILayout.Width(60));
                if (healAmount != healNode.HealAmount)
                {
                    healNode.HealAmount = healAmount;
                    GUI.changed = true;
                }
                EditorGUILayout.EndHorizontal();
                break;
                
            case DamageNode damageNode:
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Amount:", GUILayout.Width(60));
                float damageAmount = EditorGUILayout.FloatField(damageNode.DamageAmount, GUILayout.Width(60));
                if (damageAmount != damageNode.DamageAmount)
                {
                    damageNode.DamageAmount = damageAmount;
                    GUI.changed = true;
                }
                EditorGUILayout.EndHorizontal();
                break;
                
            case ApplyStatusNode statusNode:
                statusNode.StatusName = EditorGUILayout.TextField("Status:", statusNode.StatusName);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Duration:", GUILayout.Width(60));
                float statusDuration = EditorGUILayout.FloatField(statusNode.Duration, GUILayout.Width(60));
                if (statusDuration != statusNode.Duration)
                {
                    statusNode.Duration = statusDuration;
                    GUI.changed = true;
                }
                EditorGUILayout.EndHorizontal();
                break;
                
            case ConstantNode constantNode:
                constantNode.ValueType = (ConstantValueType)EditorGUILayout.EnumPopup(constantNode.ValueType);
                switch (constantNode.ValueType)
                {
                    case ConstantValueType.Int:
                        constantNode.IntValue = EditorGUILayout.IntField("Value:", constantNode.IntValue);
                        break;
                    case ConstantValueType.Float:
                        constantNode.FloatValue = EditorGUILayout.FloatField("Value:", constantNode.FloatValue);
                        break;
                    case ConstantValueType.Bool:
                        constantNode.BoolValue = EditorGUILayout.Toggle("Value:", constantNode.BoolValue);
                        break;
                    case ConstantValueType.String:
                        constantNode.StringValue = EditorGUILayout.TextField("Value:", constantNode.StringValue);
                        break;
                }
                break;
                
            case CalculateNode calcNode:
                calcNode.Operation = (CalculateOperation)EditorGUILayout.EnumPopup(calcNode.Operation);
                break;
                
            case CompareNode compareNode:
                compareNode.Operation = (CompareOperation)EditorGUILayout.EnumPopup(compareNode.Operation);
                break;
        }
    }
}

