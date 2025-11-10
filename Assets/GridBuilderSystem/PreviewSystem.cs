using UnityEngine;

namespace GridBuilder.Core
{
    public class PreviewSystem : MonoBehaviour
    {
        [SerializeField]
        private float previewYOffset = 0.00f;

        [SerializeField]
        private GameObject cellIndicator;
        private GameObject previewObject;

        [SerializeField]
        private Material previewMaterialPrefab;
        private Material previewMaterialInstance;

        private Renderer cellIndicatorRenderer;
        private Vector3Int currentObjectSize = Vector3Int.one;
        private Grid grid;

        private void Start()
        {
            previewMaterialInstance = new Material(previewMaterialPrefab);
            cellIndicator.SetActive(false);
            cellIndicatorRenderer = cellIndicator.GetComponentInChildren<Renderer>();
        }

        public void StartShowingPlacementPreview(GameObject prefab, Vector3Int size, Grid grid)
        {
            this.grid = grid;
            currentObjectSize = size;
            previewObject = Instantiate(prefab);
            PreparePreview(previewObject);
            PrepareCursor(size);
            cellIndicator.SetActive(true);
        }

        private void PrepareCursor(Vector3Int size)
        {
            if (size.x > 0 || size.y > 0 || size.z > 0)
            {
                // Cell indicator is a flat plane on the ground, so only scale X and Z (keep Y at 1)
                // Because it is a quad it uses X and Y instead of Z
                cellIndicator.transform.localScale = new Vector3(size.x, size.z, 1);
                cellIndicatorRenderer.sharedMaterial.mainTextureScale = new Vector2(size.x, size.z);
            }
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
            Color c = validity ? Color.white : Color.red;

            c.a = 0.5f;
            previewMaterialInstance.color = c;
        }

        private void ApplyFeedbackToCursor(bool validity)
        {
            Color c = validity ? Color.green : Color.red;

            c.a = 0.5f;
            cellIndicatorRenderer.sharedMaterial.color = c;
        }

        private void MoveCursor(Vector3 position)
        {
            // Position cell indicator at the same position as preview
            // The position passed in already includes offset for centering multi-cell objects
            // Cell indicator has center pivot, so it aligns correctly when positioned at the center
            cellIndicator.transform.position = new Vector3(
                position.x,
                0.01f,
                position.z);
        }

        private void MovePreview(Vector3 position)
        {
            // Preview object pivot is likely at the corner, while cell indicator pivot is at center
            // For multi-cell objects, we need to adjust the preview position to align with grid cells
            // The position passed in is centered for the cell indicator (center pivot)
            // For preview object with corner pivot, we need to offset back by half the object size
            Vector3 cellSize = grid != null ? grid.cellSize : Vector3.one;
            Vector3 pivotOffset = new Vector3(
                -(currentObjectSize.x - 1) * cellSize.x * 0.5f,
                -(currentObjectSize.y - 1) * cellSize.y * 0.5f,
                -(currentObjectSize.z - 1) * cellSize.z * 0.5f);
            
            previewObject.transform.position = new Vector3(
                position.x + pivotOffset.x,
                position.y + previewYOffset + pivotOffset.y,
                position.z + pivotOffset.z);
        }

        internal void StartShowingRemovePreview(Grid grid)
        {
            this.grid = grid;
            currentObjectSize = Vector3Int.one;
            cellIndicator.SetActive(true);
            PrepareCursor(Vector3Int.one);
            ApplyFeedbackToCursor(false);
        }
    }
}