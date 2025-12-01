       using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Nolet.Outline;
using UnityEditor;
using Monologue.Dialogue;

public class CursorManager : MonoBehaviour
{
    [SerializeField] Texture2D _DefaultCursor;
    [SerializeField] Texture2D _HoverInteractableCursor;
    [SerializeField] Texture2D _ClickInteractableCursor;
    [SerializeField] Texture2D _InvestigateCursor;
    [SerializeField] Camera _MainCamera;
    [SerializeField] LayerMask _InteractableLayer;
    public delegate void OnInteractable(GameObject interactable);
    public static event OnInteractable OnInteractableClickedEvent;

    GameObject _CurrentHoveredObject;
     void OnEnable()
    {
        SceneManager.activeSceneChanged += SetMainCamera;
    }
    void OnDisable()
    {
        SceneManager.activeSceneChanged -= SetMainCamera;
    }

    private void SetMainCamera(Scene arg0, Scene arg1)
    {
        _MainCamera = Camera.main;
    }

    void Start()
    {
        Cursor.SetCursor(_DefaultCursor, Vector2.zero, CursorMode.Auto);
    }

    void Update()
    {
        if(_MainCamera == null)
            return;
        if (Physics.Raycast(_MainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, _InteractableLayer))
        {
            GameObject hitObject = hit.collider.gameObject;
            if (Physics.Raycast(_MainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit obstacleHit, hit.distance, ~_InteractableLayer))
                return;

            if (Input.GetMouseButtonDown(0))
                Cursor.SetCursor(_ClickInteractableCursor, Vector2.zero, CursorMode.Auto);
            if (Input.GetMouseButtonUp(0))
            {
                Cursor.SetCursor(_DefaultCursor, Vector2.zero, CursorMode.Auto);
                OnInteractableClickedEvent?.Invoke(hitObject);
            }

            if (_CurrentHoveredObject == hitObject)
                return;
            Outline highlight = hitObject.AddComponent<Outline>();
            highlight.OutlineMode = Outline.Mode.OutlineAll;
            highlight.OutlineColor = Color.white;
            highlight.OutlineWidth = 15f;

            Cursor.SetCursor(_HoverInteractableCursor, Vector2.zero, CursorMode.Auto);
            _CurrentHoveredObject = hitObject;
        }
        else
        {
            Cursor.SetCursor(_DefaultCursor, Vector2.zero, CursorMode.Auto);
            if (_CurrentHoveredObject == null)
                return;
            Destroy(_CurrentHoveredObject.GetComponent<Outline>());
            _CurrentHoveredObject = null;
        }
    }
}
