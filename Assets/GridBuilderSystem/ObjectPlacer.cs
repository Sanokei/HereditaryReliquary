using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridBuilder.Core
{
    public class ObjectPlacer : MonoBehaviour
    {
        [SerializeField]
        private List<GameObject> placedGameObjects = new();

        public int PlaceObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            GameObject newObject = Instantiate(prefab);
            newObject.transform.position = position;
            newObject.transform.rotation = rotation;
            placedGameObjects.Add(newObject);
            return placedGameObjects.Count - 1;
        }
        
        // Overload for backward compatibility
        public int PlaceObject(GameObject prefab, Vector3 position)
        {
            return PlaceObject(prefab, position, Quaternion.identity);
        }

        internal void RemoveObjectAt(int gameObjectIndex)
        {
            if (placedGameObjects.Count <= gameObjectIndex
                || placedGameObjects[gameObjectIndex] == null)
                return;
            Destroy(placedGameObjects[gameObjectIndex]);
            placedGameObjects[gameObjectIndex] = null;
        }
    }
}