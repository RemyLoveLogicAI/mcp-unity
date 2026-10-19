using System.Collections.Generic;
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
                "components themselves. Does not include Unity's special DontDestroyOnLoad scene. Each " +
                "result includes both objectPath and instanceId - prefer instanceId when acting on a " +
                "result, since objectPath can be ambiguous (and rejected by path-based tools) if sibling " +
                "GameObjects share a name";
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
                    scannedObjectCount += ScanHierarchy(rootObject, rootObject.name, scene.name, results);
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
        /// Scans a root GameObject and all of its descendants (active or not) for missing script
        /// components, appending a JObject entry to <paramref name="results"/> for each affected object.
        /// Walks the hierarchy with an explicit stack rather than recursion, since a deep enough
        /// Transform chain could otherwise exhaust the call stack and crash the Editor.
        /// </summary>
        /// <returns>The number of GameObjects visited, including the root</returns>
        private static int ScanHierarchy(GameObject root, string rootPath, string sceneName, JArray results)
        {
            int visitedCount = 0;
            Stack<(GameObject gameObject, string path)> pending = new Stack<(GameObject, string)>();
            pending.Push((root, rootPath));

            while (pending.Count > 0)
            {
                (GameObject gameObject, string path) = pending.Pop();
                visitedCount++;

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
                    pending.Push((child, $"{path}/{child.name}"));
                }
            }

            return visitedCount;
        }
    }
}
