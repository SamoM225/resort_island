using System.IO;
using UnityEditor;
using UnityEngine;
using ResortIsland.Save;

namespace ResortIsland.Editor
{
    public static class SaveMenu
    {
        private const string Root = "Tools/Resort Island/Save/";

        [MenuItem(Root + "Open Save Folder")]
        private static void OpenSaveFolder()
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            EditorUtility.RevealInFinder(Path.Combine(Application.persistentDataPath, SaveSystem.FileName));
        }

        [MenuItem(Root + "Delete Save")]
        private static void DeleteSave()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Delete Save", "Stop Play mode first, otherwise the running game saves again.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete Save", "Delete the local save and all its backups? The next Play starts a new game.", "Delete", "Cancel"))
                return;

            if (!Directory.Exists(Application.persistentDataPath)) return;
            foreach (string file in Directory.GetFiles(Application.persistentDataPath, "save.*"))
                File.Delete(file);
            Debug.Log("[SaveMenu] Save deleted.");
        }
    }
}
