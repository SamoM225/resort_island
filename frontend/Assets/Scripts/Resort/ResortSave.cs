using System;
using System.Collections.Generic;

namespace ResortIsland.Resort
{
    [Serializable]
    public class BuildingSave
    {
        public string buildingId;
        public int cellX;
        public int cellY;
    }

    [Serializable]
    public class ResortSave
    {
        public List<BuildingSave> buildings = new List<BuildingSave>();
        public List<string> unlocks = new List<string>();
    }
}