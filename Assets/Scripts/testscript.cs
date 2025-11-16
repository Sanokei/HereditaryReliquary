using UnityEngine;
using GridBuilder.Core;

public class testscript : MonoBehaviour
{
    [SerializeField] BuildingSystemManager buildingSystemManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        (buildingSystemManager.GetSystemByType(BuildingSystemManager.BuildingSystem.Placement) as PlacementSystem).StartPlacement(buildingSystemManager.Databases[0], 0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
