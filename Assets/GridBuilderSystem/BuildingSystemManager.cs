using System;
using System.Collections.Generic;
using System.Linq;
using BuildingBlocks.DataTypes;
using UnityEngine;

namespace GridBuilder.Core
{
    [RequireComponent(typeof(PreviewSystem))]
    [RequireComponent(typeof(ObjectPlacer))]
    [RequireComponent(typeof(SoundFeedback))]
    public class BuildingSystemManager : MonoBehaviour
    {
        [Header("Required Components")]
        [SerializeField] private PreviewSystem previewSystem;
        [SerializeField] private ObjectPlacer objectPlacer;
        [SerializeField] private SoundFeedback soundFeedback;
        [SerializeField] private Camera sceneCamera;
        private List<SplineGridContainer> splineGridContainers = new();
        public List<ObjectsDatabaseSO> Databases = new();
        
        public enum BuildingSystem
        {
            Placement,
            Removal,
            // Add more systems here as needed
        }

        [Header("Building Systems")]
        [SerializeField] private InspectableDictionary<BuildingSystem, BaseBuildingSystem> systems = new();
        private BaseBuildingSystem activeSystem;

        // Events for grid visibility management
        public event Action<List<SplineGridContainer>> OnRequestShowGrids;
        public event Action<List<SplineGridContainer>> OnRequestHideGrids;
        public event Action OnRequestHideAllGrids;

        private void Awake()
        {
            // Find all SplineGridContainer components
            splineGridContainers = FindObjectsByType<SplineGridContainer>(FindObjectsSortMode.None).ToList();
            
            // Auto-assign required components if not set
            if (previewSystem == null)
                previewSystem = GetComponent<PreviewSystem>();
            
            if (objectPlacer == null)
                objectPlacer = GetComponent<ObjectPlacer>();
            
            if (soundFeedback == null)
                soundFeedback = GetComponent<SoundFeedback>();

            // Find camera if not assigned
            if (sceneCamera == null)
                sceneCamera = Camera.main;
                
            // Subscribe to grid visibility events
            OnRequestShowGrids += HandleShowGrids;
            OnRequestHideGrids += HandleHideGrids;
            OnRequestHideAllGrids += HandleHideAllGrids;
                
            // Initialize all systems with dependencies
            InitializeSystems();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            OnRequestShowGrids -= HandleShowGrids;
            OnRequestHideGrids -= HandleHideGrids;
            OnRequestHideAllGrids -= HandleHideAllGrids;
        }

        private void HandleShowGrids(List<SplineGridContainer> containers)
        {
            foreach (var container in containers)
            {
                if (container != null)
                    container.ShowGrid();
            }
        }

        private void HandleHideGrids(List<SplineGridContainer> containers)
        {
            foreach (var container in containers)
            {
                if (container != null)
                    container.HideGrid();
            }
        }

        private void HandleHideAllGrids()
        {
            HideAllGrids();
        }

        private void InitializeSystems()
        {
            foreach (var system in systems.Values)
            {
                if (system != null)
                {
                    // Initialize with dependencies first (pass manager reference)
                    system.Initialize(previewSystem, objectPlacer, soundFeedback, sceneCamera, this);
                    
                    // Disable system after initialization (they should be disabled by default)
                    // This ensures they're only enabled when explicitly requested
                    system.enabled = false;
                }
            }
        }

        /// <summary>
        /// Gets the currently active system
        /// </summary>
        public BaseBuildingSystem GetActiveSystem()
        {
            return activeSystem;
        }

        /// <summary>
        /// Get System by type
        /// </summary>
        public BaseBuildingSystem GetSystemByType(BuildingSystem systemType)
        {
            if (systems.TryGetValue(systemType, out var system))
            {
                return system;
            }
            Debug.LogError($"Building system of type {systemType} not found.");
            return null;
        }

        protected void HideAllGrids()
        {
            foreach (var container in splineGridContainers)
            {
                if (container != null)
                    container.HideGrid();
            }
        }

        /// <summary>
        /// Public method to request showing specific grids (called by building systems)
        /// </summary>
        public void RequestShowGrids(List<SplineGridContainer> containers)
        {
            OnRequestShowGrids?.Invoke(containers);
        }

        /// <summary>
        /// Public method to request hiding specific grids (called by building systems)
        /// </summary>
        public void RequestHideGrids(List<SplineGridContainer> containers)
        {
            OnRequestHideGrids?.Invoke(containers);
        }

        /// <summary>
        /// Public method to request hiding all grids (called by building systems)
        /// </summary>
        public void RequestHideAllGrids()
        {
            OnRequestHideAllGrids?.Invoke();
        }

        /// <summary>
        /// Gets a system of the specified type
        /// </summary>
        public T GetSystem<T>() where T : BaseBuildingSystem
        {
            foreach (var system in systems)
            {
                if (system is T typedSystem)
                {
                    return typedSystem;
                }
            }
            return null;
        }

        // Public getters for dependencies
        public PreviewSystem PreviewSystem => previewSystem;
        public ObjectPlacer ObjectPlacer => objectPlacer;
        public SoundFeedback SoundFeedback => soundFeedback;
        public Camera SceneCamera => sceneCamera;
        public List<SplineGridContainer> SplineGridContainers => splineGridContainers;

        private void OnValidate()
        {
            // Auto-assign components in editor
            if (previewSystem == null)
                previewSystem = GetComponent<PreviewSystem>();
            
            if (objectPlacer == null)
                objectPlacer = GetComponent<ObjectPlacer>();
            
            if (soundFeedback == null)
                soundFeedback = GetComponent<SoundFeedback>();

            if (sceneCamera == null)
                sceneCamera = Camera.main;
        }
    }
}

