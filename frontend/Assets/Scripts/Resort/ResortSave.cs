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
        /// 0…3, see BuildingRotation.
        public int rotation;
    }

    [Serializable]
    public class PathSave
    {
        public int cellX;
        public int cellY;
        /// PathKind (Auto / Manual; the main path is not saved, it comes from the pier).
        public int kind;
        /// Index into ResortSave.buildings of the building a generated path belongs to, -1 = none.
        public int owner = -1;
        public int entry;
        /// PathStyle id; empty = the default style.
        public string style;
        public int level = 1;
    }

    [Serializable]
    public class ResortSave
    {
        public List<BuildingSave> buildings = new List<BuildingSave>();
        public List<string> unlocks = new List<string>();
        public List<PathSave> paths = new List<PathSave>();
        /// False for saves made before paths existed - their paths are generated on load.
        public bool pathsGenerated;
        /// Bit order of PathSave.entry: 0 = old order (1 = -x, 2 = +x, 4 = -y, 8 = +y),
        /// 1 = PathFinder.Directions.
        public int pathMaskVersion;
        /// 0 = generated paths stored the old default style (they take the current default on load).
        public int pathStyleVersion;
        /// 0 = saved before buildings had a rotation and an entrance.
        public int buildingVersion;
    }
}
