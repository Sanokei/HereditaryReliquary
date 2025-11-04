using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SkillTreeEditor : EditorWindow
{
    private SkillTree skillTree;
    private SkillNode selectedNode;
    private SkillNode connectingFromNode;
    private Vector2 panOffset = Vector2.zero;
    private float zoom = 1f;
    private const float NODE_WIDTH = 200f;
    private const float NODE_HEIGHT = 100f;
    private const float CONNECTION_ARROW_SIZE = 10f;
    private const float CONNECTION_HANDLE_SIZE = 15f;
    
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
        
        // Handle node deletion
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete)
        {
            if (selectedNode != null)
            {
                DeleteNode(selectedNode);
            }
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
            skillTree.baseSkillType = (SkillType)EditorGUILayout.EnumPopup(skillTree.baseSkillType, GUILayout.Width(100));
            
            if (GUILayout.Button("Add Node", EditorStyles.toolbarButton))
            {
                AddNode();
            }
            
            if (selectedNode != null && GUILayout.Button("Delete Node", EditorStyles.toolbarButton))
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
            skillTree.treeName = "New Skill Tree";
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
        node.skill = (Skill)EditorGUILayout.ObjectField(node.skill, typeof(Skill), false);
        
        if (node.skill != null)
        {
            ISkill skillInterface = node.GetSkill();
            EditorGUILayout.LabelField(skillInterface.SkillName, EditorStyles.boldLabel);
            
            // Skill points required
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Points:", GUILayout.Width(50));
            node.skill.skillPointsRequired = EditorGUILayout.IntField(node.skill.skillPointsRequired, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
            
            // Show cooldown info if applicable
            if (skillInterface is ICooldownSkill cooldownSkill)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Cooldown:", GUILayout.Width(50));
                EditorGUILayout.LabelField($"{cooldownSkill.CooldownDuration}s", GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();
            }
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
                }
                else
                {
                    connectingFromNode = node;
                    isConnecting = true;
                }
                Event.current.Use();
            }
            else if (inputHandleRect.Contains(Event.current.mousePosition) && connectingFromNode != null && connectingFromNode != node)
            {
                skillTree.AddPrerequisite(connectingFromNode.id, node.id);
                connectingFromNode = null;
                isConnecting = false;
                GUI.changed = true;
                Event.current.Use();
            }
        }
        
        // Handle node selection and dragging
        if (Event.current.type == EventType.MouseDown && nodeRect.Contains(Event.current.mousePosition))
        {
            if (Event.current.button == 0 && !outputHandleRect.Contains(Event.current.mousePosition) && !inputHandleRect.Contains(Event.current.mousePosition))
            {
                selectedNode = node;
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
        
        if (Event.current.type == EventType.MouseUp && isConnecting)
        {
            // Check if released over another node's input handle
            foreach (var otherNode in skillTree.nodes)
            {
                if (otherNode != node)
                {
                    Vector2 otherInputPos = otherNode.position + panOffset + new Vector2(0, NODE_HEIGHT / 2);
                    Rect otherInputRect = new Rect(otherInputPos.x - CONNECTION_HANDLE_SIZE / 2, 
                                                   otherInputPos.y - CONNECTION_HANDLE_SIZE / 2, 
                                                   CONNECTION_HANDLE_SIZE, CONNECTION_HANDLE_SIZE);
                    if (otherInputRect.Contains(Event.current.mousePosition))
                    {
                        skillTree.AddPrerequisite(node.id, otherNode.id);
                        GUI.changed = true;
                        break;
                    }
                }
            }
            connectingFromNode = null;
            isConnecting = false;
            Event.current.Use();
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
        
        // Draw line
        Handles.color = Color.white;
        Handles.DrawAAPolyLine(2f, fromPos, toPos);
        
        // Draw arrow
        Vector2 direction = (toPos - fromPos).normalized;
        Vector2 arrowBase = toPos - direction * 20f;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x) * CONNECTION_ARROW_SIZE;
        
        Handles.DrawAAPolyLine(2f, arrowBase + perpendicular, toPos);
        Handles.DrawAAPolyLine(2f, arrowBase - perpendicular, toPos);
        
        // Draw connection handle for deletion
        Vector2 midPoint = (fromPos + toPos) / 2;
        Rect handleRect = new Rect(midPoint.x - 10, midPoint.y - 10, 20, 20);
        
        if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && handleRect.Contains(Event.current.mousePosition))
        {
            skillTree.RemovePrerequisite(from.id, to.id);
            GUI.changed = true;
            Event.current.Use();
        }
    }
    
    private void DrawConnectionPreview()
    {
        if (connectingFromNode == null) return;
        
        Vector2 fromPos = connectingFromNode.position + panOffset + new Vector2(NODE_WIDTH, NODE_HEIGHT / 2);
        Vector2 toPos = Event.current.mousePosition;
        
        Handles.BeginGUI();
        Handles.color = Color.yellow;
        Handles.DrawAAPolyLine(2f, fromPos, toPos);
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
            isPanning = false;
            isDragging = false;
            e.Use();
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
            e.Use();
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

