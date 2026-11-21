using System;
using System.Collections.Generic;
using UnityEngine;
using ResortIsland.Waves;

namespace ResortIsland.UI
{

    [Serializable]
    public class NeedRowData
    {
        [SerializeField] private string label = "Rooms";
        [SerializeField, Min(0)] private int served = 4;
        [SerializeField, Min(0)] private int total = 8;
        [SerializeField, Min(0)] private int queue = 0;
        [SerializeField, Min(0)] private int queueWarningThreshold = 5;

        public string Label => label;
        public int Served => served;
        public int Total => total;
        public int Queue => queue;
        public int QueueWarningThreshold => queueWarningThreshold;
    }

    public class NeedsPanel : MonoBehaviour, INeedsView
    {
        private const int DefaultWarningThreshold = 5;

        [Header("Rows and dummy data (same order)")]
        [SerializeField] private NeedRowView[] rows;
        [SerializeField] private NeedRowData[] data;

        private bool live;
        private int thresholdOverride;

        private void Start() => Refresh();

        
        public void SetWarningThreshold(int threshold) => thresholdOverride = threshold;

        
        public void Show(IReadOnlyList<NeedRowState> needs)
        {
            live = true;
            if (rows == null) return;

            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null) continue;

                bool hasNeed = needs != null && i < needs.Count;
                rows[i].gameObject.SetActive(hasNeed);
                if (!hasNeed) continue;

                NeedRowState need = needs[i];
                rows[i].Set(need.Name, need.Served, need.Total, need.Queue, need.Queue > WarningThresholdFor(i));
            }
        }

        private int WarningThresholdFor(int index)
        {
            if (thresholdOverride > 0)
                return thresholdOverride;

            return data != null && index < data.Length && data[index] != null
                ? data[index].QueueWarningThreshold
                : DefaultWarningThreshold;
        }

        [ContextMenu("Refresh")]
        public void Refresh()
        {
            if (live) return;
            if (rows == null || data == null) return;

            int count = Mathf.Min(rows.Length, data.Length);
            for (int i = 0; i < count; i++)
            {
                if (rows[i] == null || data[i] == null) continue;

                var d = data[i];
                rows[i].Set(d.Label, d.Served, d.Total, d.Queue, d.Queue > d.QueueWarningThreshold);
            }
        }

#if UNITY_EDITOR
       
        private void OnValidate() => UnityEditor.EditorApplication.delayCall += DelayedRefresh;

        private void DelayedRefresh()
        {
            UnityEditor.EditorApplication.delayCall -= DelayedRefresh;
            if (this != null) Refresh();
        }
#endif
    }
}