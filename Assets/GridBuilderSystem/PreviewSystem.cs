using System.Collections.Generic;
using UnityEngine;

namespace GridBuilder.Core
{
    public class PreviewSystem : MonoBehaviour
    {
        [SerializeField] private float previewYOffset = 0.00f;
        [SerializeField, Range(0f, 1f)] private float previewOpacity = 0.2f;
        [SerializeField] private Color previewColor = new Color(1f, 1f, 1f);
        [SerializeField] private Color invalidPreviewColor = new Color(1f, 0f, 0f);
        [SerializeField] private Color validCellIndicatorColor = new Color(0f, 1f, 0f);
        [SerializeField] private Color invalidCellIndicatorColor = new Color(1f, 0f, 0f);
        private Material previewMaterialInstance;

        private GameObject cellIndicator;
        private GameObject previewObject;
        private Renderer cellIndicatorRenderer;
        private List<Vector3Int> currentOccupiedCells = new List<Vector3Int> { Vector3Int.zero };
        private Grid grid;
        private float currentRotation = 0f;
        private MeshFilter cellIndicatorMeshFilter;

        private void Awake()
        {
            previewMaterialInstance = new Material(Shader.Find("Sprites/Default"));
            previewMaterialInstance.color = new Color(previewColor.r, previewColor.g, previewColor.b, previewOpacity);
            previewMaterialInstance.SetFloat("_Mode", 0);
            previewMaterialInstance.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            previewMaterialInstance.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            previewMaterialInstance.SetInt("_ZWrite", 0);
            previewMaterialInstance.DisableKeyword("_ALPHATEST_ON");
            previewMaterialInstance.EnableKeyword("_ALPHABLEND_ON");
            previewMaterialInstance.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            previewMaterialInstance.renderQueue = 3000;
            CreateCellIndicator();
        }
        
        private void CreateCellIndicator()
        {
            if (cellIndicator != null)
                Destroy(cellIndicator);
                
            cellIndicator = new GameObject("CellIndicator");
            cellIndicator.transform.SetParent(transform);
            cellIndicator.transform.localPosition = Vector3.zero;
            cellIndicator.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            
            cellIndicatorMeshFilter = cellIndicator.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = cellIndicator.AddComponent<MeshRenderer>();
            
            // Create a material for the cell indicator
            Material cellMaterial = new Material(Shader.Find("Sprites/Default"));
            cellMaterial.color = new Color(validCellIndicatorColor.r, validCellIndicatorColor.g, validCellIndicatorColor.b);
            meshRenderer.material = cellMaterial;
            cellIndicatorRenderer = meshRenderer;
            
            cellIndicator.SetActive(false);
        }

        public void StartShowingPlacementPreview(GameObject prefab, List<Vector3Int> occupiedCells, Grid grid)
        {
            this.grid = grid;
            currentOccupiedCells = new List<Vector3Int>(occupiedCells);
            currentRotation = 0f;
            previewObject = Instantiate(prefab);
            PreparePreview(previewObject);
            PrepareCursor(occupiedCells);
            
            // Reset rotation for both preview and cell indicator
            Quaternion identityRotation = Quaternion.identity;
            if (previewObject != null)
            {
                previewObject.transform.rotation = identityRotation;
            }
            
            cellIndicator.SetActive(true);
        }
        
        public void SetRotation(float yRotation)
        {
            currentRotation = yRotation;
            Quaternion rotation = Quaternion.Euler(0, currentRotation, 0);
            
            if (previewObject != null)
            {
                previewObject.transform.rotation = rotation;
            }
            
            // Regenerate cell indicator mesh with rotated cells
            if (cellIndicator != null && currentOccupiedCells != null)
            {
                List<Vector3Int> rotatedCells = RotateOccupiedCells(currentOccupiedCells, currentRotation);
                PrepareCursor(rotatedCells);
            }
        }
        
        private List<Vector3Int> RotateOccupiedCells(List<Vector3Int> cells, float yRotation)
        {
            // Normalize rotation to 0, 90, 180, 270 degrees
            int rotationSteps = Mathf.RoundToInt(yRotation / 90f) % 4;
            if (rotationSteps < 0) rotationSteps += 4;
            
            if (rotationSteps == 0)
                return new List<Vector3Int>(cells);
            
            if (cells == null || cells.Count == 0)
                return new List<Vector3Int>(cells);
            
            // Find bounding box to calculate center
            int minX = int.MaxValue, minY = int.MaxValue, minZ = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue, maxZ = int.MinValue;
            
            foreach (var cell in cells)
            {
                minX = Mathf.Min(minX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                minZ = Mathf.Min(minZ, cell.z);
                maxX = Mathf.Max(maxX, cell.x);
                maxY = Mathf.Max(maxY, cell.y);
                maxZ = Mathf.Max(maxZ, cell.z);
            }
            
            // Calculate center of bounding box (as float for accuracy)
            // Then round to nearest integer to use as pivot point
            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;
            float centerZ = (minZ + maxZ) * 0.5f;
            
            Vector3Int pivot = new Vector3Int(
                Mathf.RoundToInt(centerX),
                Mathf.RoundToInt(centerY),
                Mathf.RoundToInt(centerZ)
            );
            
            List<Vector3Int> rotatedCells = new List<Vector3Int>();
            
            foreach (var cell in cells)
            {
                // Translate to origin relative to pivot
                Vector3Int relative = cell - pivot;
                
                // Apply 90-degree rotations counter-clockwise: (x, z) -> (-z, x)
                int rotatedX = relative.x;
                int rotatedZ = relative.z;
                
                for (int i = 0; i < rotationSteps; i++)
                {
                    int temp = rotatedX;
                    rotatedX = -rotatedZ;
                    rotatedZ = temp;
                }
                
                // Translate back relative to pivot
                Vector3Int rotated = new Vector3Int(rotatedX, relative.y, rotatedZ) + pivot;
                rotatedCells.Add(rotated);
            }
            
            return rotatedCells;
        }
        
        public float GetRotation()
        {
            return currentRotation;
        }

        private void PrepareCursor(List<Vector3Int> occupiedCells)
        {
            // Generate a combined mesh from all occupied cells
            Mesh combinedMesh = GenerateCombinedCellMesh(occupiedCells);
            if (cellIndicatorMeshFilter != null)
            {
                // Clear old mesh if it exists
                if (cellIndicatorMeshFilter.sharedMesh != null && cellIndicatorMeshFilter.sharedMesh.name == "CombinedCellIndicator")
                {
                    DestroyImmediate(cellIndicatorMeshFilter.sharedMesh);
                }
                cellIndicatorMeshFilter.mesh = combinedMesh;
            }
        }
        
        private Mesh GenerateCombinedCellMesh(List<Vector3Int> occupiedCells)
        {
            Mesh mesh = new Mesh();
            mesh.name = "CombinedCellIndicator";
            
            if (occupiedCells == null || occupiedCells.Count == 0)
            {
                return mesh;
            }
            
            Vector3 cellSize = grid != null ? grid.cellSize : Vector3.one;
            
            // Calculate the center of all occupied cells (in cell space)
            // This ensures the mesh is centered at the origin, so when positioned it aligns correctly
            Vector3 cellCenterOffset = Vector3.zero;
            foreach (var cell in occupiedCells)
            {
                cellCenterOffset += new Vector3(cell.x, cell.y, cell.z);
            }
            cellCenterOffset /= occupiedCells.Count;
            
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            
            // Create a quad for each occupied cell
            // Quads are created in the XZ plane (horizontal, facing up)
            for (int i = 0; i < occupiedCells.Count; i++)
            {
                Vector3Int cell = occupiedCells[i];
                // Offset cell position by the center so the mesh is centered at origin
                Vector3 relativeCellPos = new Vector3(
                    (cell.x - cellCenterOffset.x) * cellSize.x,
                    0,
                    (cell.z - cellCenterOffset.z) * cellSize.z
                );
                
                // Quad vertices (slightly above ground to avoid z-fighting)
                float yOffset = 0.02f;
                float halfX = cellSize.x * 0.5f;
                float halfZ = cellSize.z * 0.5f;
                
                int vertexOffset = vertices.Count;
                
                // Four corners of the quad in XZ plane (horizontal)
                vertices.Add(relativeCellPos + new Vector3(-halfX, yOffset, -halfZ));
                vertices.Add(relativeCellPos + new Vector3(halfX, yOffset, -halfZ));
                vertices.Add(relativeCellPos + new Vector3(halfX, yOffset, halfZ));
                vertices.Add(relativeCellPos + new Vector3(-halfX, yOffset, halfZ));
                
                // UVs
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 1));
                
                // Triangles (two triangles per quad)
                triangles.Add(vertexOffset + 0);
                triangles.Add(vertexOffset + 2);
                triangles.Add(vertexOffset + 1);
                
                triangles.Add(vertexOffset + 0);
                triangles.Add(vertexOffset + 3);
                triangles.Add(vertexOffset + 2);
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }

        private void PreparePreview(GameObject previewObject)
        {
            previewObject.transform.localScale = previewObject.transform.localScale * 1.01f;
            Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = previewMaterialInstance;
                }
                renderer.sharedMaterials = materials;
            }
        }

        public void StopShowingPreview()
        {
            cellIndicator.SetActive(false);
            if (previewObject != null)
                Destroy(previewObject);
        }

        public void UpdatePosition(Vector3 position, bool validity)
        {
            if (previewObject != null)
            {
                MovePreview(position);
                ApplyFeedbackToPreview(validity);

            }

            MoveCursor(position);
            ApplyFeedbackToCursor(validity);
        }

        private void ApplyFeedbackToPreview(bool validity)
        {
            Color c = validity ? previewColor : invalidPreviewColor;

            c.a = previewOpacity;
            previewMaterialInstance.color = c;
        }

        private void ApplyFeedbackToCursor(bool validity)
        {
            Color c = validity ? validCellIndicatorColor : invalidCellIndicatorColor;
            cellIndicatorRenderer.sharedMaterial.color = c;
        }

        private void MoveCursor(Vector3 position)
        {
            if (cellIndicator == null)
                return;
                
            // Position cell indicator at the same position as preview
            // The position passed in already includes offset for centering multi-cell objects
            // Cell indicator has center pivot, so it aligns correctly when positioned at the center
            // Use a higher Y offset to prevent z-fighting with the grid visualization mesh
            cellIndicator.transform.position = new Vector3(
                position.x,
                0.02f, // Increased from 0.01f to sit above grid mesh without obvious gap
                position.z);
        }

        private void MovePreview(Vector3 position)
        {
            // Preview object pivot should align with grid position
            // The position passed in is the grid cell center
            previewObject.transform.position = new Vector3(
                position.x,
                position.y + previewYOffset,
                position.z);
        }

        internal void StartShowingRemovePreview(Grid grid)
        {
            this.grid = grid;
            currentOccupiedCells = new List<Vector3Int> { Vector3Int.zero };
            cellIndicator.SetActive(true);
            PrepareCursor(currentOccupiedCells);
            ApplyFeedbackToCursor(false);
        }
    }
}