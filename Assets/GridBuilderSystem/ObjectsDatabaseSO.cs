using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridBuilder.Core
{
    [CreateAssetMenu]
    public class ObjectsDatabaseSO : ScriptableObject
    {
        public LayerMask placementLayermask;
        public List<ObjectData> objectsData;
    }

    [Serializable]
    public class ObjectData
    {
        [field: SerializeField]
        public string Name { get; private set; }
        [field: SerializeField]
        public int ID { get; private set; }
        [field: SerializeField]
        public List<Vector3Int> OccupiedCells { get; private set; } = new List<Vector3Int> { Vector3Int.zero };
        [field: SerializeField]
        public GameObject Prefab { get; private set; }
    }
}