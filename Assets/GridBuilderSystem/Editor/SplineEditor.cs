using UnityEngine;
using UnityEngine.Splines;
using UnityEditor;
using UnityEditor.Splines;

namespace GridBuilder.Core
{
    /// <summary>
    /// Editor script that enforces rectangular constraints on spline editing.
    /// Ensures splines form rectangular polygons with no slopes.
    /// </summary>
    [CustomEditor(typeof(SplineGridContainer))]
    public class SplineEditor : Editor
    {
        private SplineGridContainer splineGridContainer;
        private SplineContainer splineContainer;
        
        private void OnEnable()
        {
            splineGridContainer = (SplineGridContainer)target;
            splineContainer = splineGridContainer.SplineContainer;
            
            // Subscribe to spline changes to automatically enforce constraints
            if (splineContainer != null && splineContainer.Spline != null)
            {
                Spline.Changed += OnSplineChanged;
            }
        }
        
        private void OnDisable()
        {
            // Unsubscribe from spline changes
            Spline.Changed -= OnSplineChanged;
        }
        
        private void OnSplineChanged(Spline spline, int knotIndex, SplineModification modification)
        {
            // Only enforce constraints for the spline associated with this container
            if (splineContainer != null && splineContainer.Spline == spline && (
                    modification == SplineModification.KnotInserted ||
                    modification == SplineModification.KnotModified ||
                    modification == SplineModification.KnotReordered ||
                    modification == SplineModification.KnotRemoved)
                )
            {
                // Use EditorApplication.delayCall to enforce constraints after the current frame
                // This ensures the spline modification is complete before we enforce constraints
                EditorApplication.delayCall += () => EnforceRectangularConstraints(spline);
            }
        }
        
        private void EnforceRectangularConstraints(Spline spline)
        {
            if (spline == null || spline.Count < 2)
                return;
                
            Undo.RecordObject(splineContainer, "Enforce Rectangular Constraints");
            bool needsUpdate = false;
            
            // First knot can be anywhere
            if (spline.Count == 1)
                return;
                
            // Second knot must be on X or Z axis of first knot
            if (spline.Count >= 2)
            {
                var firstKnot = spline[0];
                var secondKnot = spline[1];
                
                Vector3 firstPos = firstKnot.Position;
                Vector3 secondPos = secondKnot.Position;
                
                // Check if second knot is aligned on X or Z axis
                bool alignedX = Mathf.Approximately(firstPos.z, secondPos.z);
                bool alignedZ = Mathf.Approximately(firstPos.x, secondPos.x);
                
                if (!alignedX && !alignedZ)
                {
                    // Force alignment to nearest axis
                    float deltaX = Mathf.Abs(secondPos.x - firstPos.x);
                    float deltaZ = Mathf.Abs(secondPos.z - firstPos.z);
                    
                    if (deltaX > deltaZ)
                    {
                        // Align to X axis
                        secondPos.z = firstPos.z;
                    }
                    else
                    {
                        // Align to Z axis
                        secondPos.x = firstPos.x;
                    }
                    
                    spline.SetKnot(1, new BezierKnot(secondPos));
                    needsUpdate = true;
                }
            }
            
            // For subsequent knots, ensure rectangular shape
            for (int i = 2; i < spline.Count; i++)
            {
                var prevKnot = spline[i - 1];
                var currentKnot = spline[i];
                
                Vector3 prevPos = prevKnot.Position;
                Vector3 currentPos = currentKnot.Position;
                
                // Determine direction from previous knot
                var prevPrevKnot = spline[i - 2];
                Vector3 prevPrevPos = prevPrevKnot.Position;
                
                // Check if previous movement was on X or Z axis
                bool prevMovedX = !Mathf.Approximately(prevPos.x, prevPrevPos.x);
                bool prevMovedZ = !Mathf.Approximately(prevPos.z, prevPrevPos.z);
                
                // Current knot must move perpendicular to previous movement
                if (prevMovedX)
                {
                    // Previous was X movement, current must be Z movement
                    currentPos.x = prevPos.x;
                    if (Mathf.Approximately(currentPos.z, prevPos.z))
                    {
                        // If no Z movement detected, keep current Z but ensure it's different
                        // This handles the case where user hasn't moved yet
                        float zDiff = Mathf.Abs(currentPos.z - prevPos.z);
                        if (zDiff < 0.01f)
                        {
                            // Use a default step based on X direction
                            currentPos.z = prevPos.z + (prevPos.x > prevPrevPos.x ? 1f : -1f);
                        }
                    }
                }
                else if (prevMovedZ)
                {
                    // Previous was Z movement, current must be X movement
                    currentPos.z = prevPos.z;
                    if (Mathf.Approximately(currentPos.x, prevPos.x))
                    {
                        // If no X movement detected, keep current X but ensure it's different
                        float xDiff = Mathf.Abs(currentPos.x - prevPos.x);
                        if (xDiff < 0.01f)
                        {
                            // Use a default step based on Z direction
                            currentPos.x = prevPos.x + (prevPos.z > prevPrevPos.z ? 1f : -1f);
                        }
                    }
                }
                
                if (Vector3.Distance(currentPos, currentKnot.Position) > 0.01f)
                {
                    spline.SetKnot(i, new BezierKnot(currentPos));
                    needsUpdate = true;
                }
            }
            
            // Ensure last knot connects to first knot (closed loop)
            if (spline.Closed && spline.Count > 2)
            {
                var firstKnot = spline[0];
                var lastKnot = spline[spline.Count - 1];
                
                Vector3 firstPos = firstKnot.Position;
                Vector3 lastPos = lastKnot.Position;
                
                // Check if last knot needs to be adjusted to match first
                if (!Mathf.Approximately(lastPos.x, firstPos.x) || 
                    !Mathf.Approximately(lastPos.z, firstPos.z))
                {
                    // Determine which axis to align based on previous knot
                    if (spline.Count > 1)
                    {
                        var prevKnot = spline[spline.Count - 2];
                        Vector3 prevPos = prevKnot.Position;
                        
                        bool prevMovedX = !Mathf.Approximately(lastPos.x, prevPos.x);
                        
                        if (prevMovedX)
                        {
                            // Last movement was X, align Z to first, then X
                            lastPos.z = firstPos.z;
                            lastPos.x = firstPos.x;
                        }
                        else
                        {
                            // Last movement was Z, align X to first, then Z
                            lastPos.x = firstPos.x;
                            lastPos.z = firstPos.z;
                        }
                    }
                    else
                    {
                        // Just align both
                        lastPos.x = firstPos.x;
                        lastPos.z = firstPos.z;
                    }
                    
                    spline.SetKnot(spline.Count - 1, new BezierKnot(lastPos));
                    needsUpdate = true;
                }
            }
            
            if (needsUpdate)
            {
                EditorUtility.SetDirty(splineContainer);
            }
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            if (splineContainer == null)
            {
                EditorGUILayout.HelpBox("Spline Container component is required on the same GameObject!", MessageType.Warning);
                return;
            }
            
            if (splineContainer.Spline == null)
            {
                EditorGUILayout.HelpBox("Spline is required!", MessageType.Warning);
                return;
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spline Constraints", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Rectangular constraints are automatically enforced when editing knots:\n" +
                "- After first knot, second knot must be on X or Z axis\n" +
                "- Subsequent knots must form rectangular shape\n" +
                "- Last knot must connect to first knot when closed",
                MessageType.Info);
        }
    }
}

