using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for scanning loaded scenes for GameObjects with missing (deleted/renamed) script
    /// components - the most common silent breakage after an automated script or asset edit
    /// </summary>
    public class FindMissingReferencesTool : McpToolBase
    {
        public FindMissingReferencesTool()
        {
            Name = "find_missing_references";
            Description = "Scans all loaded scenes for GameObjects with missing (deleted/renamed) script " +
                "components, to catch silent breakage after a script or asset edit. Does not check " +
                "serialized object-reference fields (e.g. a null Material slot) - only missing script " +
                "components themselves";
        }

        public override JObject Execute(JObject parameters)
        {
            JArray results = new JArray();
            int scannedObjectCount = 0;

            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (GameObject rootObject in scene.GetRootGameObjects())
                {
                    scannedObjectCount += ScanGameObject(rootObject, rootObject.name, scene.name, results);
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = results.Count > 0
                    ? $"Found {results.Count} GameObject(s) with missing script components (scanned {scannedObjectCount} objects)"
                    : $"No missing script components found (scanned {scannedObjectCount} objects)",
                ["scannedObjectCount"] = scannedObjectCount,
                ["results"] = results
            };
        }

        /// <summary>
        /// Recursively scans a GameObject and its children (active or not) for missing script
        /// components, appending a JObject entry to <paramref name="results"/> for each affected object.
        /// </summary>
        /// <returns>The number of GameObjects visited, including this one</returns>
        private static int ScanGameObject(GameObject gameObject, string path, string sceneName, JArray results)
        {
            int visitedCount = 1;

            Component[] components = gameObject.GetComponents<Component>();
            int missingCount = 0;
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    missingCount++;
                }
            }

            if (missingCount > 0)
            {
                results.Add(new JObject
                {
                    ["objectPath"] = path,
                    ["instanceId"] = gameObject.GetInstanceID(),
                    ["sceneName"] = sceneName,
                    ["missingComponentCount"] = missingCount
                });
            }

            Transform transform = gameObject.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject child = transform.GetChild(i).gameObject;
                visitedCount += ScanGameObject(child, $"{path}/{child.name}", sceneName, results);
            }

            return visitedCount;
        }
    }
}
