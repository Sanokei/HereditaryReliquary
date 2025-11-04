using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

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

    // Reflection constructor cache for dynamic node creation
    private readonly Dictionary<Type, ConstructorInfo> nodeConstructorCache = new Dictionary<Type, ConstructorInfo>();
    
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
        
        // Mark assets as dirty when changed
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
    
    private void AutoSaveAssets()
    {
        if (selectedSkill != null && AssetDatabase.Contains(selectedSkill))
        {
            AssetDatabase.SaveAssets();
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
        
        Vector2 newNodePos = Event.current.mousePosition - panOffset + new Vector2(NODE_WIDTH * 0.6f, 0f);
        SkillGraphNode newNode = InstantiateNode(nodeType, newNodePos);
        
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
            AutoSaveAssets();
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
        bool isEntryNode = node is EntryNode;
        Vector2 nodePos = node.Position + panOffset;
        
        // Entry node is thinner
        float nodeHeight = isEntryNode ? NODE_HEIGHT * 0.6f : NODE_HEIGHT;
        Rect nodeRect = new Rect(nodePos.x, nodePos.y, NODE_WIDTH, nodeHeight);
        
        if (isEntryNode)
        {
            // Draw special Entry node with rounded corners and green color
            DrawEntryNode(nodeRect, node == selectedNode);
        }
        else
        {
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
        }
        
        // Draw connection handles on the sides
        Vector2 outputHandlePos = nodePos + new Vector2(NODE_WIDTH, nodeHeight / 2);
        Vector2 inputHandlePos = nodePos + new Vector2(0, nodeHeight / 2);
        
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
        
        // Draw input handle (left side) - red (skip for Entry node)
        if (!isEntryNode)
        {
            Handles.BeginGUI();
            Handles.color = Color.red;
            Handles.DrawSolidDisc(inputHandlePos, Vector3.forward, CONNECTION_HANDLE_SIZE / 2);
            Handles.color = Color.white;
            Handles.EndGUI();
        }
        
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
            else if (!isEntryNode && inputHandleRect.Contains(Event.current.mousePosition) && connectingFromNode != null && connectingFromNode != node)
            {
                // Complete connection to first execution input pin (Entry node has no input)
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
        // Only start dragging when clicking the header area (title bar), not the property area
        // and only when no GUI control has captured the event
        Rect headerRect = new Rect(nodeRect.x, nodeRect.y, nodeRect.width, 24f);
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && headerRect.Contains(Event.current.mousePosition) && GUIUtility.hotControl == 0 && !EditorGUIUtility.editingTextField)
        {
            bool clickedOnHandle = outputHandleRect.Contains(Event.current.mousePosition);
            if (!isEntryNode)
            {
                clickedOnHandle = clickedOnHandle || inputHandleRect.Contains(Event.current.mousePosition);
            }
            
            if (!clickedOnHandle)
            {
                selectedNode = node;
                isDragging = true;
                GUI.FocusControl(null);
                GUIUtility.keyboardControl = 0;
                lastMousePosition = Event.current.mousePosition;
                Event.current.Use();
            }
        }
        
        if (isDragging && selectedNode == node)
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
    }
    
    // All old property drawer methods removed - they're now in DrawNodePropertiesInNode
    
    private void DrawEntryNode(Rect nodeRect, bool isSelected)
    {
        // Define green color scheme
        Color mainGreen = new Color(0.2f, 0.7f, 0.3f, 1f);
        Color borderColor = isSelected ? Color.yellow : new Color(0.1f, 0.5f, 0.2f, 1f);
        
        // Corner radius for rounded rectangle
        float cornerRadius = 10f;
        
        Handles.BeginGUI();
        
        // Draw main background (center rectangle)
        Rect centerRect = new Rect(nodeRect.x + cornerRadius, nodeRect.y, 
                                    nodeRect.width - cornerRadius * 2, nodeRect.height);
        EditorGUI.DrawRect(centerRect, mainGreen);
        
        // Draw top and bottom rectangles (vertical parts)
        Rect topRect = new Rect(nodeRect.x, nodeRect.y + cornerRadius, 
                               nodeRect.width, nodeRect.height - cornerRadius * 2);
        EditorGUI.DrawRect(topRect, mainGreen);
        
        // Draw corner circles
        Vector2 topLeft = new Vector2(nodeRect.x + cornerRadius, nodeRect.y + cornerRadius);
        Vector2 topRight = new Vector2(nodeRect.x + nodeRect.width - cornerRadius, nodeRect.y + cornerRadius);
        Vector2 bottomLeft = new Vector2(nodeRect.x + cornerRadius, nodeRect.y + nodeRect.height - cornerRadius);
        Vector2 bottomRight = new Vector2(nodeRect.x + nodeRect.width - cornerRadius, nodeRect.y + nodeRect.height - cornerRadius);
        
        Handles.color = mainGreen;
        Handles.DrawSolidDisc(topLeft, Vector3.forward, cornerRadius);
        Handles.DrawSolidDisc(topRight, Vector3.forward, cornerRadius);
        Handles.DrawSolidDisc(bottomLeft, Vector3.forward, cornerRadius);
        Handles.DrawSolidDisc(bottomRight, Vector3.forward, cornerRadius);
        
        // Draw rounded border using Bezier curves
        float borderWidth = isSelected ? 4f : 3f;
        Handles.color = borderColor;
        
        // Top edge
        Vector2 topLeftEdge = new Vector2(nodeRect.x + cornerRadius, nodeRect.y);
        Vector2 topRightEdge = new Vector2(nodeRect.x + nodeRect.width - cornerRadius, nodeRect.y);
        Handles.DrawAAPolyLine(borderWidth, topLeftEdge, topRightEdge);
        
        // Bottom edge
        Vector2 bottomLeftEdge = new Vector2(nodeRect.x + cornerRadius, nodeRect.y + nodeRect.height);
        Vector2 bottomRightEdge = new Vector2(nodeRect.x + nodeRect.width - cornerRadius, nodeRect.y + nodeRect.height);
        Handles.DrawAAPolyLine(borderWidth, bottomLeftEdge, bottomRightEdge);
        
        // Left edge
        Vector2 leftTopEdge = new Vector2(nodeRect.x, nodeRect.y + cornerRadius);
        Vector2 leftBottomEdge = new Vector2(nodeRect.x, nodeRect.y + nodeRect.height - cornerRadius);
        Handles.DrawAAPolyLine(borderWidth, leftTopEdge, leftBottomEdge);
        
        // Right edge
        Vector2 rightTopEdge = new Vector2(nodeRect.x + nodeRect.width, nodeRect.y + cornerRadius);
        Vector2 rightBottomEdge = new Vector2(nodeRect.x + nodeRect.width, nodeRect.y + nodeRect.height - cornerRadius);
        Handles.DrawAAPolyLine(borderWidth, rightTopEdge, rightBottomEdge);
        
        // Draw rounded corners using Bezier curves (using 0.55 constant for circular approximation)
        float bezierConstant = 0.55f;
        
        // Top-left corner
        Vector2 tlStart = topLeftEdge;
        Vector2 tlEnd = leftTopEdge;
        Vector2 tlControl1 = new Vector2(tlStart.x - cornerRadius * bezierConstant, tlStart.y);
        Vector2 tlControl2 = new Vector2(tlEnd.x, tlEnd.y - cornerRadius * bezierConstant);
        Handles.DrawBezier(tlStart, tlEnd, tlControl1, tlControl2, borderColor, null, borderWidth);
        
        // Top-right corner
        Vector2 trStart = topRightEdge;
        Vector2 trEnd = rightTopEdge;
        Vector2 trControl1 = new Vector2(trStart.x + cornerRadius * bezierConstant, trStart.y);
        Vector2 trControl2 = new Vector2(trEnd.x, trEnd.y - cornerRadius * bezierConstant);
        Handles.DrawBezier(trStart, trEnd, trControl1, trControl2, borderColor, null, borderWidth);
        
        // Bottom-left corner
        Vector2 blStart = leftBottomEdge;
        Vector2 blEnd = bottomLeftEdge;
        Vector2 blControl1 = new Vector2(blStart.x, blStart.y + cornerRadius * bezierConstant);
        Vector2 blControl2 = new Vector2(blEnd.x - cornerRadius * bezierConstant, blEnd.y);
        Handles.DrawBezier(blStart, blEnd, blControl1, blControl2, borderColor, null, borderWidth);
        
        // Bottom-right corner
        Vector2 brStart = rightBottomEdge;
        Vector2 brEnd = bottomRightEdge;
        Vector2 brControl1 = new Vector2(brStart.x, brStart.y + cornerRadius * bezierConstant);
        Vector2 brControl2 = new Vector2(brEnd.x + cornerRadius * bezierConstant, brEnd.y);
        Handles.DrawBezier(brStart, brEnd, brControl1, brControl2, borderColor, null, borderWidth);
        
        Handles.color = Color.white;
        Handles.EndGUI();
        
        // Draw "ENTRY" text centered (lock all states to white so no hover color change)
        GUIStyle textStyle = new GUIStyle(EditorStyles.boldLabel);
        textStyle.alignment = TextAnchor.MiddleCenter;
        textStyle.fontSize = 16;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.normal.textColor = Color.white;
        textStyle.hover.textColor = Color.white;
        textStyle.active.textColor = Color.white;
        textStyle.focused.textColor = Color.white;
        
        Rect textRect = nodeRect;
        
        GUI.Label(textRect, "ENTRY", textStyle);
    }
    
    private float GetNodeRenderHeight(SkillGraphNode node)
    {
        return (node is EntryNode) ? NODE_HEIGHT * 0.6f : NODE_HEIGHT;
    }
    
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
        float fromHeight = GetNodeRenderHeight(fromNode);
        float toHeight = GetNodeRenderHeight(toNode);
        Vector2 fromPos = fromNode.Position + panOffset + new Vector2(NODE_WIDTH, fromHeight / 2f);
        Vector2 toPos = toNode.Position + panOffset + new Vector2(0, toHeight / 2f);
        
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
        
        float fromHeight = GetNodeRenderHeight(connectingFromNode);
        Vector2 fromPos = connectingFromNode.Position + panOffset + new Vector2(NODE_WIDTH, fromHeight / 2f);
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
        
        // Repaint when dragging during connection
        if (isConnecting && e.type == EventType.MouseDrag)
        {
            Repaint();
        }
        
        if (e.type == EventType.MouseUp && (e.button == 2 || e.button == 0))
        {
            bool handledMouseUp = false;
            // Handle connection completion on mouse up
            if (isConnecting && connectingFromNode != null)
            {
                bool connectionMade = false;
                
                // Check if released over another node's input handle
                if (skillGraph != null && skillGraph.Nodes != null)
                {
                    foreach (var node in skillGraph.Nodes)
                    {
                        if (node != connectingFromNode)
                        {
                            float inHeight = GetNodeRenderHeight(node);
                            Vector2 inputPos = node.Position + panOffset + new Vector2(0, inHeight / 2f);
                            Rect inputRect = new Rect(inputPos.x - CONNECTION_HANDLE_SIZE / 2, 
                                                      inputPos.y - CONNECTION_HANDLE_SIZE / 2, 
                                                      CONNECTION_HANDLE_SIZE, CONNECTION_HANDLE_SIZE);
                            if (inputRect.Contains(e.mousePosition))
                            {
                                // Complete connection to first execution input pin
                                var inputPins = node.GetInputPins();
                                var execInputPins = inputPins.Where(p => p.Type == PinType.Execution).ToList();
                                if (execInputPins.Count > 0)
                                {
                                    if (connectingFromNode.AddConnection(connectingFromPinId, skillGraph, node.Id, execInputPins[0].Id))
                                    {
                                        connectionMade = true;
                                        // Deselect node when creating connection
                                        selectedNode = null;
                                        GUI.changed = true;
                                    }
                                    else
                                    {
                                        Debug.LogWarning("Cannot create connection: Type mismatch or invalid connection");
                                        connectingFromNode = null;
                                        connectingFromPinId = null;
                                        isConnecting = false;
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
                
                // Cancel connection if not over another node
                if (!connectionMade)
                {
                    connectingFromNode = null;
                    connectingFromPinId = null;
                    isConnecting = false;
                }
                else
                {
                    connectingFromNode = null;
                    connectingFromPinId = null;
                    isConnecting = false;
                }
                handledMouseUp = true;
            }
            
            isPanning = false;
            isDragging = false;
            
            // Auto-save after mouse up (when user finishes dragging/editing)
            if (GUI.changed)
            {
                AutoSaveAssets();
                handledMouseUp = true;
            }

            if (handledMouseUp)
            {
                e.Use();
            }
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
                    float nHeight = GetNodeRenderHeight(node);
                    Rect nodeRect = new Rect(nodePos.x, nodePos.y, NODE_WIDTH, nHeight);
                    
                    if (nodeRect.Contains(e.mousePosition))
                    {
                        clickedOnNode = true;
                        break;
                    }
                    
                    // Check handles
                    Vector2 outputHandlePos = nodePos + new Vector2(NODE_WIDTH, nHeight / 2f);
                    Vector2 inputHandlePos = nodePos + new Vector2(0, nHeight / 2f);
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
            AutoSaveAssets();
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
        // Special handling for ConstantNode (has conditional fields based on ValueType)
        if (node is ConstantNode constantNode)
        {
            DrawConstantNodeProperties(constantNode);
            return;
        }
        
        // Respect Inspector-like layout
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 110f;
        
        // Prefer serialized fields first (to honor attributes), then properties without corresponding fields
        var fieldsToDraw = GetDrawableFields(node);
        var drawnMemberNames = new HashSet<string>();
        
        foreach (var field in fieldsToDraw)
        {
            DrawMemberWithAttributes(node, field, isProperty: false);
            drawnMemberNames.Add(field.Name);
            // Also consider likely matching property name (PascalCase)
            string possibleProp = char.ToUpper(field.Name[0]) + field.Name.Substring(1);
            drawnMemberNames.Add(possibleProp);
        }
        
        var propertiesToDraw = GetDrawableProperties(node)
            .Where(p => !drawnMemberNames.Contains(p.Name))
            .ToList();
        foreach (var prop in propertiesToDraw)
        {
            DrawMemberWithAttributes(node, prop, isProperty: true);
        }
        
        EditorGUIUtility.labelWidth = oldLabelWidth;
    }
    
    private void DrawMemberWithAttributes(SkillGraphNode node, MemberInfo member, bool isProperty)
    {
        // Header/Space support
        var headerAttr = member.GetCustomAttribute<HeaderAttribute>(false);
        if (headerAttr != null)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(headerAttr.header, EditorStyles.boldLabel);
        }
        var spaceAttr = member.GetCustomAttribute<SpaceAttribute>(false);
        if (spaceAttr != null)
        {
            EditorGUILayout.Space(spaceAttr.height);
        }
        
        // Tooltip and range/text area metadata
        var tooltipAttr = member.GetCustomAttribute<TooltipAttribute>(false);
        var rangeAttr = member.GetCustomAttribute<RangeAttribute>(false);
        var textAreaAttr = member.GetCustomAttribute<TextAreaAttribute>(false);
        var minAttr = member.GetCustomAttribute<MinAttribute>(false);
        
        System.Type memberType = isProperty ? ((PropertyInfo)member).PropertyType : ((FieldInfo)member).FieldType;
        string memberName = member.Name;
        string labelText = FormatDisplayName(memberName);
        GUIContent label = string.IsNullOrEmpty(tooltipAttr?.tooltip) ? new GUIContent(labelText) : new GUIContent(labelText, tooltipAttr.tooltip);
        
        EditorGUI.BeginChangeCheck();
        object currentValue = isProperty ? ((PropertyInfo)member).GetValue(node) : ((FieldInfo)member).GetValue(node);
        object newValue = DrawFieldValueWithAttributes(memberType, label, currentValue, rangeAttr, textAreaAttr, minAttr);
        if (EditorGUI.EndChangeCheck())
        {
            if (isProperty)
            {
                ((PropertyInfo)member).SetValue(node, newValue);
            }
            else
            {
                ((FieldInfo)member).SetValue(node, newValue);
            }
            GUI.changed = true;
        }
    }
    
    private object DrawFieldValueWithAttributes(System.Type type, GUIContent label, object value, RangeAttribute range, TextAreaAttribute textArea, MinAttribute min)
    {
        // Handle attributes first where applicable (Range, TextArea)
        if (type == typeof(int))
        {
            int v = value != null ? (int)value : 0;
            if (range != null)
            {
                v = EditorGUILayout.IntSlider(label, v, Mathf.RoundToInt(range.min), Mathf.RoundToInt(range.max));
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.Width(110f));
                v = EditorGUILayout.IntField(GUIContent.none, v);
                EditorGUILayout.EndHorizontal();
            }
            if (min != null) v = Mathf.Max((int)min.min, v);
            return v;
        }
        if (type == typeof(float))
        {
            float v = value != null ? (float)value : 0f;
            if (range != null)
            {
                v = EditorGUILayout.Slider(label, v, range.min, range.max);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.Width(110f));
                v = EditorGUILayout.FloatField(GUIContent.none, v);
                EditorGUILayout.EndHorizontal();
            }
            if (min != null) v = Mathf.Max(min.min, v);
            return v;
        }
        if (type == typeof(bool))
        {
            return EditorGUILayout.Toggle(label, value != null ? (bool)value : false);
        }
        if (type == typeof(string))
        {
            string v = value as string ?? string.Empty;
            if (textArea != null)
            {
                int minLines = Mathf.Max(1, textArea.minLines);
                int maxLines = Mathf.Max(minLines, textArea.maxLines);
                // Draw a text area with min/max height similar to inspector
                var style = EditorStyles.textArea;
                float lineHeight = style.lineHeight > 0 ? style.lineHeight : 14f;
                float minHeight = lineHeight * minLines + 8f;
                float maxHeight = lineHeight * maxLines + 8f;
                Rect r = GUILayoutUtility.GetRect(new GUIContent(v), style, GUILayout.ExpandWidth(true), GUILayout.MinHeight(minHeight), GUILayout.MaxHeight(maxHeight));
                v = EditorGUI.TextArea(r, v, style);
            }
            else
            {
                v = EditorGUILayout.TextField(label, v);
            }
            return v;
        }
        if (type == typeof(Color))
        {
            Color c = value != null ? (Color)value : Color.white;
            return EditorGUILayout.ColorField(label, c);
        }
        if (type == typeof(Vector2))
        {
            Vector2 v = value != null ? (Vector2)value : Vector2.zero;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(110f));
            v = EditorGUILayout.Vector2Field(GUIContent.none, v);
            EditorGUILayout.EndHorizontal();
            return v;
        }
        if (type == typeof(Vector3))
        {
            Vector3 v = value != null ? (Vector3)value : Vector3.zero;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(110f));
            v = EditorGUILayout.Vector3Field(GUIContent.none, v);
            EditorGUILayout.EndHorizontal();
            return v;
        }
        if (type == typeof(Vector4))
        {
            Vector4 v = value != null ? (Vector4)value : Vector4.zero;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(110f));
            v = EditorGUILayout.Vector4Field(GUIContent.none, v);
            EditorGUILayout.EndHorizontal();
            return v;
        }
        if (type == typeof(Quaternion))
        {
            Quaternion q = value != null ? (Quaternion)value : Quaternion.identity;
            Vector3 euler = q.eulerAngles;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(110f));
            euler = EditorGUILayout.Vector3Field(GUIContent.none, euler);
            EditorGUILayout.EndHorizontal();
            return Quaternion.Euler(euler);
        }
        if (type == typeof(AnimationCurve))
        {
            AnimationCurve curve = value as AnimationCurve ?? AnimationCurve.Linear(0, 0, 1, 1);
            return EditorGUILayout.CurveField(label, curve);
        }
        if (typeof(UnityEngine.Object).IsAssignableFrom(type))
        {
            return EditorGUILayout.ObjectField(label, value as UnityEngine.Object, type, true);
        }
        
        // Fallback to previous generic drawer
        return DrawFieldValue(type, label.text, value);
    }
    
    private List<PropertyInfo> GetDrawableProperties(SkillGraphNode node)
    {
        var properties = new List<PropertyInfo>();
        var nodeType = node.GetType();
        
        // Get all public properties that have getters and setters from the declared type only
        foreach (var prop in nodeType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            // Skip properties that shouldn't be shown
            if (ShouldSkipProperty(prop))
                continue;
                
            // Only include properties with both getter and setter
            if (prop.CanRead && prop.CanWrite)
            {
                properties.Add(prop);
            }
        }
        
        return properties;
    }
    
    private List<FieldInfo> GetDrawableFields(SkillGraphNode node)
    {
        var fields = new List<FieldInfo>();
        var nodeType = node.GetType();
        
        // Get all fields (public or with [SerializeField]) from the declared type only
        foreach (var field in nodeType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            // Skip private fields without [SerializeField]
            if (field.IsPrivate && !field.IsDefined(typeof(SerializeField), false))
                continue;
                
            // Skip fields that shouldn't be shown
            if (ShouldSkipField(field))
                continue;
                
            fields.Add(field);
        }
        
        return fields;
    }
    
    private bool ShouldSkipProperty(PropertyInfo prop)
    {
        // Skip base class properties
        string[] skipNames = { "Id", "Position", "Title", "Description" };
        return skipNames.Contains(prop.Name);
    }
    
    private bool ShouldSkipField(FieldInfo field)
    {
        // Skip base class fields
        string[] skipNames = { "id", "position", "nodeTitle", "nodeDescription", "pinConnections" };
        return skipNames.Contains(field.Name);
    }
    
    private void DrawFieldOrProperty(SkillGraphNode node, MemberInfo member, bool isProperty)
    {
        System.Type memberType = isProperty ? ((PropertyInfo)member).PropertyType : ((FieldInfo)member).FieldType;
        string memberName = member.Name;
        
        // Make a nice display name
        string displayName = FormatDisplayName(memberName);
        
        EditorGUI.BeginChangeCheck();
        object currentValue = isProperty ? ((PropertyInfo)member).GetValue(node) : ((FieldInfo)member).GetValue(node);
        object newValue = DrawFieldValue(memberType, displayName, currentValue);
        
        if (EditorGUI.EndChangeCheck())
        {
            if (isProperty)
            {
                ((PropertyInfo)member).SetValue(node, newValue);
            }
            else
            {
                ((FieldInfo)member).SetValue(node, newValue);
            }
            GUI.changed = true;
        }
    }
    
    private string FormatDisplayName(string name)
    {
        // Remove common suffixes
        if (name.EndsWith("Value"))
            name = name.Substring(0, name.Length - 5);
        if (name.EndsWith("Count"))
            name = name.Substring(0, name.Length - 5);
        if (name.EndsWith("Amount"))
            name = name.Substring(0, name.Length - 6);
        if (name.EndsWith("Duration"))
            name = name.Substring(0, name.Length - 8);
            
        // Convert camelCase to Title Case
        if (string.IsNullOrEmpty(name))
            return name;
            
        StringBuilder result = new StringBuilder();
        result.Append(char.ToUpper(name[0]));
        for (int i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                result.Append(' ');
            }
            result.Append(name[i]);
        }
        
        return result.ToString() + ":";
    }
    
    private object DrawFieldValue(System.Type type, string label, object value)
    {
        if (type == typeof(int))
        {
            return EditorGUILayout.IntField(label, value != null ? (int)value : 0, GUILayout.Width(NODE_WIDTH - 20));
        }
        else if (type == typeof(float))
        {
            return EditorGUILayout.FloatField(label, value != null ? (float)value : 0f, GUILayout.Width(NODE_WIDTH - 20));
        }
        else if (type == typeof(bool))
        {
            return EditorGUILayout.Toggle(label, value != null ? (bool)value : false);
        }
        else if (type == typeof(string))
        {
            return EditorGUILayout.TextField(label, value != null ? (string)value : "");
        }
        else if (type == typeof(Vector2))
        {
            return EditorGUILayout.Vector2Field(label, value != null ? (Vector2)value : Vector2.zero);
        }
        else if (type == typeof(Vector3))
        {
            return EditorGUILayout.Vector3Field(label, value != null ? (Vector3)value : Vector3.zero);
        }
        else if (type.IsEnum)
        {
            return EditorGUILayout.EnumPopup(label, value != null ? (System.Enum)value : System.Enum.GetValues(type).GetValue(0) as System.Enum);
        }
        else if (type == typeof(UnityEngine.Object) || type.IsSubclassOf(typeof(UnityEngine.Object)))
        {
            return EditorGUILayout.ObjectField(label, value as UnityEngine.Object, type, true);
        }
        else
        {
            // Unknown type, skip it
            return value;
        }
    }
    
    private void DrawConstantNodeProperties(ConstantNode constantNode)
    {
        // Draw ValueType first
        EditorGUI.BeginChangeCheck();
        constantNode.ValueType = (ConstantValueType)EditorGUILayout.EnumPopup("Value Type:", constantNode.ValueType);
        if (EditorGUI.EndChangeCheck())
        {
            GUI.changed = true;
        }
        
        // Draw the appropriate value field based on ValueType
        EditorGUI.BeginChangeCheck();
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
            case ConstantValueType.Vector3:
                constantNode.Vector3Value = EditorGUILayout.Vector3Field("Value:", constantNode.Vector3Value);
                break;
        }
        if (EditorGUI.EndChangeCheck())
        {
            GUI.changed = true;
        }
    }

    private SkillGraphNode InstantiateNode(System.Type nodeType, Vector2 position)
    {
        if (nodeType == null) return null;

        // Try to get cached Vector2 constructor
        if (!nodeConstructorCache.TryGetValue(nodeType, out ConstructorInfo cachedCtor))
        {
            // Prefer ctor(Vector2)
            cachedCtor = nodeType.GetConstructor(new Type[] { typeof(Vector2) });
            // Cache (may be null if not present)
            nodeConstructorCache[nodeType] = cachedCtor;
        }

        try
        {
            if (cachedCtor != null)
            {
                return (SkillGraphNode)cachedCtor.Invoke(new object[] { position });
            }

            // Fallback to parameterless constructor
            var node = (SkillGraphNode)Activator.CreateInstance(nodeType);
            if (node != null)
            {
                node.Position = position;
            }
            return node;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to instantiate node of type {nodeType?.Name}: {ex.Message}");
            return null;
        }
    }
}

