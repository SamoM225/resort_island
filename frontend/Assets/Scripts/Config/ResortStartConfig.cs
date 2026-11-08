using System;
using UnityEngine;

namespace ResortIsland.Config
{
    [Serializable]
    public class StartingBuilding
    {
        [SerializeField] private BuildingConfig building;
        [SerializeField] private Vector3Int cell;

        public BuildingConfig Building => building;
        public Vector3Int Cell => cell;
    }

    [CreateAssetMenu(fileName = "ResortStartConfig", menuName = "ResortIsland/Config/ResortStartConfig")]
    public class ResortStartConfig : ScriptableObject
    {
        [Header("Buildings (static positions at start)")]
        [SerializeField] private StartingBuilding[] startingBuildings;

        [Header("Needs")]
        [SerializeField] private NeedConfig[] activeNeeds;

        [Header("Workers")]
        [SerializeField] private int startingWorkers = 2;
        [SerializeField] private GameObject workerPrefab;
        [SerializeField] private Vector3Int workerStartCell = new Vector3Int(-2, -2, 0);

        public StartingBuilding[] StartingBuildings => startingBuildings;
        public NeedConfig[] ActiveNeeds => activeNeeds;
        public int StartingWorkers => startingWorkers;
        public GameObject WorkerPrefab => workerPrefab;
        public Vector3Int WorkerStartCell => workerStartCell;
    }
}