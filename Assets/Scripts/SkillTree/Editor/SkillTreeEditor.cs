using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using SkillSystem.Core;

public class SkillTreeEditor : EditorWindow
{
    private SkillTree skillTree;
    private SkillNode selectedNode;
    private SkillNode connectingFromNode;
    private string selectedConnectionFromId;
    private string selectedConnectionToId;
    private Vector2 panOffset = Vector2.zero;
    private float zoom = 1f;
    private const float NODE_WIDTH = 200f;
    private const float NODE_HEIGHT = 100f;
    private const float CONNECTION_ARROW_SIZE = 10f;
    private const float CONNECTION_HANDLE_SIZE = 15f;
    private const float CONNECTION_LINE_WIDTH = 6f;
    private const float CONNECTION_SELECTION_WIDTH = 8f;
    
    private Vector2 lastMousePosition;
    private bool isDragging = false;
    private bool isPanning = false;
    private bool isConnecting = false;
    
    [MenuItem("Tools/Skill Tree Editor")]
    public static void ShowWindow()
    {
        GetWindow<SkillTreeEditor>("Skill Tree Editor");
    }
    
    private void OnEnable()
    {
        // Load the last selected skill tree if available
    }
    
    private void OnGUI()
    {
        DrawToolbar();
        
        if (skillTree == null)
        {
            DrawEmptyState();
            return;
        }
        
        HandleInput();
        
        // Draw background
        DrawGrid(20, 0.2f, Color.gray);
        DrawGrid(100, 0.4f, Color.gray);
        
        // Draw connections
        DrawConnections();
        
        // Draw nodes
        DrawNodes();
        
        // Draw connection preview
        if (connectingFromNode != null)
        {
            DrawConnectionPreview();
        }
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(skillTree);
        }
    }
    
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        // Load skill tree
        EditorGUILayout.LabelField("Skill Tree:", GUILayout.Width(70));
        skillTree = (SkillTree)EditorGUILayout.ObjectField(skillTree, typeof(SkillTree), false, GUILayout.Width(200));
        
        GUILayout.FlexibleSpace();
        
        // Base skill type filter
        if (skillTree != null)
        {
            EditorGUILayout.LabelField("Base Type:", GUILayout.Width(70));
            selectedNode.skill.type = (SSSkill.SkillType)EditorGUILayout.EnumPopup(selectedNode.GetSkill().type, GUILayout.Width(100));

            if (GUILayout.Button("Add Node", EditorStyles.toolbarButton))
            {
                AddNode();
            }
            
            if (selectedNode != null && GUILayout.Button("Delete Node", EditorStyles.toolbarButton))
            {
                DeleteNode(selectedNode);
            }
            
            if (!string.IsNullOrEmpty(selectedConnectionFromId) && !string.IsNullOrEmpty(selectedConnectionToId))
            {
                if (GUILayout.Button("Delete Connection", EditorStyles.toolbarButton))
                {
                    skillTree.RemovePrerequisite(selectedConnectionFromId, selectedConnectionToId);
                    selectedConnectionFromId = null;
                    selectedConnectionToId = null;
                    GUI.changed = true;
                }
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawEmptyState()
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.Space(50);
        EditorGUILayout.LabelField("No Skill Tree Selected", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(10);
        if (GUILayout.Button("Create New Skill Tree", GUILayout.Height(30)))
        {
            CreateNewSkillTree();
        }
        EditorGUILayout.EndVertical();
    }
    
    private void CreateNewSkillTree()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create Skill Tree", "NewSkillTree", "asset", "Create a new Skill Tree");
        if (!string.IsNullOrEmpty(path))
        {
            skillTree = CreateInstance<SkillTree>();
            skillTree.nodes = new List<SkillNode>();
            AssetDatabase.CreateAsset(skillTree, path);
            AssetDatabase.SaveAssets();
        }
    }
    
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
    
    private void DrawNodes()
    {
        if (skillTree == null || skillTree.nodes == null) return;
        
        foreach (var node in skillTree.nodes)
        {
            DrawNode(node);
        }
    }
    
    private void DrawNode(SkillNode node)
    {
        Vector2 nodePos = node.position + panOffset;
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
        
        // Draw node content
        GUILayout.BeginArea(nodeRect);
        EditorGUILayout.BeginVertical();
        
        // Skill reference
        node.skill = (SSSkill)EditorGUILayout.ObjectField(node.skill, typeof(SSSkill), false);
        
        if (node.skill != null)
        {
            SSSkill skillInterface = node.GetSkill();
            EditorGUILayout.LabelField(skillInterface.skillName, EditorStyles.boldLabel);
            
            // Skill points required (more prominent with slider)
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Skill Points:", GUILayout.Width(80));
            EditorGUI.BeginChangeCheck();
            // node.skill.skillPointsRequired = EditorGUILayout.IntSlider(node.skill.skillPointsRequired, 1, 100);
            if (EditorGUI.EndChangeCheck())
            {
                GUI.changed = true;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cooldown:", GUILayout.Width(50));
            EditorGUILayout.LabelField($"{selectedNode.GetSkill().skillCooldown}s", GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.LabelField("No Skill Assigned", EditorStyles.centeredGreyMiniLabel);
        }
        
        // Show connection info
        if (node.prerequisites != null && node.prerequisites.Count > 0)
        {
            EditorGUILayout.LabelField($"Prerequisites: {node.prerequisites.Count}", EditorStyles.miniLabel);
        }
        
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
        
        // Draw connection handles
        Vector2 outputHandlePos = nodePos + new Vector2(NODE_WIDTH, NODE_HEIGHT / 2);
        Vector2 inputHandlePos = nodePos + new Vector2(0, NODE_HEIGHT / 2);
        
        Rect outputHandleRect = new Rect(outputHandlePos.x - CONNECTION_HANDLE_SIZE / 2, 
                                        outputHandlePos.y - CONNECTION_HANDLE_SIZE / 2, 
                                        CONNECTION_HANDLE_SIZE, CONNECTION_HANDLE_SIZE);
        Rect inputHandleRect = new Rect(inputHandlePos.x - CONNECTION_HANDLE_SIZE / 2, 
                                       inputHandlePos.y - CONNECTION_HANDLE_SIZE / 2, 
                                       CONNECTION_HANDLE_SIZE, CONNECTION_HANDLE_SIZE);
        
        // Draw output handle (right side)
        Handles.BeginGUI();
        Handles.color = (connectingFromNode == node) ? Color.yellow : Color.green;
        Handles.DrawSolidDisc(outputHandlePos, Vector3.forward, CONNECTION_HANDLE_SIZE / 2);
        Handles.color = Color.white;
        Handles.EndGUI();
        
        // Draw input handle (left side)
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
                if (connectingFromNode == node)
                {
                    connectingFromNode = null;
                    isConnecting = false;
                }
                else
                {
                    connectingFromNode = node;
                    isConnecting = true;
                    // Deselect node and connection when starting connection
                    selectedNode = null;
                    selectedConnectionFromId = null;
                    selectedConnectionToId = null;
                }
                Event.current.Use();
            }
            else if (inputHandleRect.Contains(Event.current.mousePosition) && connectingFromNode != null && connectingFromNode != node)
            {
                if (skillTree.AddPrerequisite(connectingFromNode.id, node.id))
                {
                    connectingFromNode = null;
                    isConnecting = false;
                    // Deselect node when creating connection
                    selectedNode = null;
                    GUI.changed = true;
                }
                else
                {
                    // Would create a cycle or invalid connection
                    Debug.LogWarning($"Cannot create prerequisite: This would create a circular dependency in the skill tree.");
                    connectingFromNode = null;
                    isConnecting = false;
                }
                Event.current.Use();
            }
        }
        
        // Handle node selection and dragging (single click to select)
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && nodeRect.Contains(Event.current.mousePosition))
        {
            if (!outputHandleRect.Contains(Event.current.mousePosition) && !inputHandleRect.Contains(Event.current.mousePosition))
            {
                selectedNode = node;
                selectedConnectionFromId = null; // Deselect connection when selecting node
                selectedConnectionToId = null;
                isDragging = true;
                lastMousePosition = Event.current.mousePosition;
                Event.current.Use();
            }
        }
        
        if (isDragging && selectedNode == node)
        {
            if (Event.current.type == EventType.MouseDrag)
            {
                Vector2 delta = Event.current.mousePosition - lastMousePosition;
                node.position += delta;
                lastMousePosition = Event.current.mousePosition;
                GUI.changed = true;
                Event.current.Use();
            }
        }
        
        // Check if dragging to another node's input handle
        if (isConnecting && connectingFromNode == node && Event.current.type == EventType.MouseDrag)
        {
            Repaint();
        }
    }
    
    private void DrawConnections()
    {
        if (skillTree == null || skillTree.nodes == null) return;
        
        Handles.BeginGUI();
        
        foreach (var node in skillTree.nodes)
        {
            if (node.prerequisites == null) continue;
            
            foreach (var prereqId in node.prerequisites)
            {
                SkillNode prereqNode = skillTree.GetNode(prereqId);
                if (prereqNode != null)
                {
                    DrawConnection(prereqNode, node);
                }
            }
        }
        
        Handles.EndGUI();
    }
    
    private void DrawConnection(SkillNode from, SkillNode to)
    {
        Vector2 fromPos = from.position + panOffset + new Vector2(NODE_WIDTH, NODE_HEIGHT / 2);
        Vector2 toPos = to.position + panOffset + new Vector2(0, NODE_HEIGHT / 2);
        
        bool isSelected = (selectedConnectionFromId == from.id && selectedConnectionToId == to.id);
        
        // Draw selection highlight if selected
        if (isSelected)
        {
            Handles.color = Color.yellow;
            Handles.DrawAAPolyLine(CONNECTION_SELECTION_WIDTH + 2f, fromPos, toPos);
        }
        
        // Draw line (thicker)
        Handles.color = isSelected ? Color.yellow : Color.white;
        Handles.DrawAAPolyLine(CONNECTION_LINE_WIDTH, fromPos, toPos);
        
        // Draw arrow (thicker)
        Vector2 direction = (toPos - fromPos).normalized;
        Vector2 arrowBase = toPos - direction * 20f;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x) * CONNECTION_ARROW_SIZE;
        
        Handles.DrawAAPolyLine(CONNECTION_LINE_WIDTH, arrowBase + perpendicular, toPos);
        Handles.DrawAAPolyLine(CONNECTION_LINE_WIDTH, arrowBase - perpendicular, toPos);
        
        // Check if connection is clicked for selection
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            Vector2 mousePos = Event.current.mousePosition;
            
            // Check if click is on the output handle (green dot) for this specific connection
            Vector2 fromNodePos = from.position + panOffset;
            Vector2 outputHandlePos = fromNodePos + new Vector2(NODE_WIDTH, NODE_HEIGHT / 2);
            Rect outputHandleRect = new Rect(outputHandlePos.x - CONNECTION_HANDLE_SIZE / 2, 
                                            outputHandlePos.y - CONNECTION_HANDLE_SIZE / 2, 
                                            CONNECTION_HANDLE_SIZE, CONNECTION_HANDLE_SIZE);
            bool clickedOnOutputHandle = outputHandleRect.Contains(mousePos);
            
            // Check if click is on the input handle (red dot) for this specific connection
            Vector2 toNodePos = to.position + panOffset;
            Vector2 inputHandlePos = toNodePos + new Vector2(0, NODE_HEIGHT / 2);
            Rect inputHandleRect = new Rect(inputHandlePos.x - CONNECTION_HANDLE_SIZE / 2, 
                                           inputHandlePos.y - CONNECTION_HANDLE_SIZE / 2, 
                                           CONNECTION_HANDLE_SIZE, CONNECTION_HANDLE_SIZE);
            bool clickedOnInputHandle = inputHandleRect.Contains(mousePos);
            
            // Only select if clicking on the line itself, not on the handles
            if (!clickedOnOutputHandle && !clickedOnInputHandle)
            {
                // Check if click is near the line segment (not just near endpoints)
                float distanceToLine = DistanceToLineSegment(mousePos, fromPos, toPos);
                
                // Also check that the click is actually along the line segment, not just near the endpoints
                // Calculate the closest point on the line segment
                Vector2 line = toPos - fromPos;
                float lineLength = line.magnitude;
                if (lineLength > 0.001f)
                {
                    Vector2 lineNormalized = line / lineLength;
                    Vector2 pointToStart = mousePos - fromPos;
                    float t = Vector2.Dot(pointToStart, lineNormalized);
                    
                    // Only consider clicks that are actually along the line segment (not too close to endpoints)
                    // Exclude a buffer zone near the handles
                    float handleBuffer = CONNECTION_HANDLE_SIZE * 1.5f;
                    if (t > handleBuffer && t < lineLength - handleBuffer && distanceToLine < 15f)
                    {
                        selectedConnectionFromId = from.id;
                        selectedConnectionToId = to.id;
                        selectedNode = null; // Deselect node when selecting connection
                        connectingFromNode = null; // Deselect any in-progress connection
                        isConnecting = false;
                        GUI.changed = true;
                        Event.current.Use();
                    }
                }
            }
        }
        
        // Right-click to delete (old behavior preserved)
        Vector2 midPoint = (fromPos + toPos) / 2;
        Rect handleRect = new Rect(midPoint.x - 10, midPoint.y - 10, 20, 20);
        
        if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && handleRect.Contains(Event.current.mousePosition))
        {
            skillTree.RemovePrerequisite(from.id, to.id);
            if (selectedConnectionFromId == from.id && selectedConnectionToId == to.id)
            {
                selectedConnectionFromId = null;
                selectedConnectionToId = null;
            }
            GUI.changed = true;
            Event.current.Use();
        }
    }
    
    /// <summary>
    /// Calculates the distance from a point to a line segment
    /// </summary>
    private float DistanceToLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 line = lineEnd - lineStart;
        float lineLength = line.magnitude;
        
        if (lineLength < 0.001f)
            return Vector2.Distance(point, lineStart);
        
        Vector2 lineNormalized = line / lineLength;
        Vector2 pointToStart = point - lineStart;
        
        float t = Mathf.Clamp01(Vector2.Dot(pointToStart, lineNormalized));
        Vector2 closestPoint = lineStart + lineNormalized * t;
        
        return Vector2.Distance(point, closestPoint);
    }
    
    private void DrawConnectionPreview()
    {
        if (connectingFromNode == null) return;
        
        Vector2 fromPos = connectingFromNode.position + panOffset + new Vector2(NODE_WIDTH, NODE_HEIGHT / 2);
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
    
    private void HandleInput()
    {
        Event e = Event.current;
        
        // Panning with middle mouse or space
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
            // Handle connection completion on mouse up
            if (isConnecting && connectingFromNode != null)
            {
                bool connectionMade = false;
                
                // Check if released over another node's input handle
                foreach (var node in skillTree.nodes)
                {
                    if (node != connectingFromNode)
                    {
                        Vector2 inputPos = node.position + panOffset + new Vector2(0, NODE_HEIGHT / 2);
                        Rect inputRect = new Rect(inputPos.x - CONNECTION_HANDLE_SIZE / 2, 
                                                  inputPos.y - CONNECTION_HANDLE_SIZE / 2, 
                                                  CONNECTION_HANDLE_SIZE, CONNECTION_HANDLE_SIZE);
                        if (inputRect.Contains(e.mousePosition))
                        {
                            if (skillTree.AddPrerequisite(connectingFromNode.id, node.id))
                            {
                                connectionMade = true;
                                // Deselect node when creating connection
                                selectedNode = null;
                                GUI.changed = true;
                            }
                            else
                            {
                                // Would create a cycle or invalid connection
                                Debug.LogWarning($"Cannot create prerequisite: This would create a circular dependency in the skill tree.");
                                connectingFromNode = null;
                                isConnecting = false;
                            }
                            break;
                        }
                    }
                }
                
                // Cancel connection if not over another node
                if (!connectionMade)
                {
                    connectingFromNode = null;
                    isConnecting = false;
                }
                else
                {
                    connectingFromNode = null;
                    isConnecting = false;
                }
            }
            
            isPanning = false;
            isDragging = false;
            e.Use();
        }
        
        // Double-click on background to create new node
        if (e.type == EventType.MouseDown && e.button == 0 && e.clickCount == 2)
        {
            // Check if clicking on background (not on any node)
            bool clickedOnNode = false;
            foreach (var node in skillTree.nodes)
            {
                Vector2 nodePos = node.position + panOffset;
                Rect nodeRect = new Rect(nodePos.x, nodePos.y, NODE_WIDTH, NODE_HEIGHT);
                if (nodeRect.Contains(e.mousePosition))
                {
                    clickedOnNode = true;
                    break;
                }
            }
            
            if (!clickedOnNode && !isConnecting && !isPanning)
            {
                // Calculate position relative to pan offset
                Vector2 newNodePos = e.mousePosition - panOffset - new Vector2(NODE_WIDTH / 2, NODE_HEIGHT / 2);
                SkillNode newNode = new SkillNode(newNodePos);
                skillTree.AddNode(newNode);
                selectedNode = newNode;
                GUI.changed = true;
                e.Use();
            }
        }
        
        // Zoom with scroll wheel
        if (e.type == EventType.ScrollWheel)
        {
            zoom += e.delta.y * 0.01f;
            zoom = Mathf.Clamp(zoom, 0.5f, 2f);
            e.Use();
        }
        
        // Right click to cancel connection
        if (e.type == EventType.MouseDown && e.button == 1 && connectingFromNode != null)
        {
            connectingFromNode = null;
            isConnecting = false;
            e.Use();
        }
        
        // Cancel connection on escape
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            connectingFromNode = null;
            isConnecting = false;
            // Also deselect connections
            selectedConnectionFromId = null;
            selectedConnectionToId = null;
            e.Use();
        }
        
        // Handle node and connection deletion with Delete key
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete)
        {
            if (selectedNode != null)
            {
                DeleteNode(selectedNode);
                e.Use();
            }
            else if (!string.IsNullOrEmpty(selectedConnectionFromId) && !string.IsNullOrEmpty(selectedConnectionToId))
            {
                skillTree.RemovePrerequisite(selectedConnectionFromId, selectedConnectionToId);
                selectedConnectionFromId = null;
                selectedConnectionToId = null;
                GUI.changed = true;
                e.Use();
            }
        }
        
        // Deselect everything on background click (single click)
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            // Check if clicking on background (not on any node or connection)
            bool clickedOnNode = false;
            bool clickedOnConnection = false;
            
            // Check nodes (including handles)
            foreach (var node in skillTree.nodes)
            {
                Vector2 nodePos = node.position + panOffset;
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
            
            // Check connections (rough check - if near any connection line)
            if (!clickedOnNode)
            {
                foreach (var node in skillTree.nodes)
                {
                    if (node.prerequisites == null) continue;
                    foreach (var prereqId in node.prerequisites)
                    {
                        SkillNode prereqNode = skillTree.GetNode(prereqId);
                        if (prereqNode != null)
                        {
                            Vector2 fromPos = prereqNode.position + panOffset + new Vector2(NODE_WIDTH, NODE_HEIGHT / 2);
                            Vector2 toPos = node.position + panOffset + new Vector2(0, NODE_HEIGHT / 2);
                            float distanceToLine = DistanceToLineSegment(e.mousePosition, fromPos, toPos);
                            // Exclude clicks near handles
                            Vector2 line = toPos - fromPos;
                            float lineLength = line.magnitude;
                            if (lineLength > 0.001f)
                            {
                                Vector2 lineNormalized = line / lineLength;
                                Vector2 pointToStart = e.mousePosition - fromPos;
                                float t = Vector2.Dot(pointToStart, lineNormalized);
                                float handleBuffer = CONNECTION_HANDLE_SIZE * 1.5f;
                                if (t > handleBuffer && t < lineLength - handleBuffer && distanceToLine < 20f)
                                {
                                    clickedOnConnection = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (clickedOnConnection) break;
                }
            }
            
            // If clicking on background, deselect everything (don't consume event so node selection can still work)
            if (!clickedOnNode && !clickedOnConnection && !isConnecting && !isPanning)
            {
                selectedConnectionFromId = null;
                selectedConnectionToId = null;
                selectedNode = null; // Deselect nodes
                GUI.changed = true;
                // Don't consume event - let other handlers process it if needed
            }
        }
    }
    
    private void AddNode()
    {
        SkillNode newNode = new SkillNode(new Vector2(position.width / 2 - NODE_WIDTH / 2, position.height / 2 - NODE_HEIGHT / 2));
        skillTree.AddNode(newNode);
        selectedNode = newNode;
        GUI.changed = true;
    }
    
    private void DeleteNode(SkillNode node)
    {
        if (skillTree != null && node != null)
        {
            skillTree.RemoveNode(node.id);
            if (selectedNode == node)
            {
                selectedNode = null;
            }
            if (connectingFromNode == node)
            {
                connectingFromNode = null;
            }
            GUI.changed = true;
        }
    }
}

