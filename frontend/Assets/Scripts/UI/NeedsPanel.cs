using System;
using UnityEngine;

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

    public class NeedsPanel : MonoBehaviour
    {
        [Header("Rows and dummy data (same order)")]
        [SerializeField] private NeedRowView[] rows;
        [SerializeField] private NeedRowData[] data;

        private void Start() => Refresh();

        [ContextMenu("Refresh")]
        public void Refresh()
        {
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
        // zmena čísla v Inspectore sa hneď prejaví v paneli
        private void OnValidate() => UnityEditor.EditorApplication.delayCall += DelayedRefresh;

        private void DelayedRefresh()
        {
            UnityEditor.EditorApplication.delayCall -= DelayedRefresh;
            if (this != null) Refresh();
        }
#endif
    }
}