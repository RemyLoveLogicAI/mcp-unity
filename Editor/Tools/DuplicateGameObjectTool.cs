using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for duplicating a GameObject in the Unity scene by path or instance ID
    /// </summary>
    public class DuplicateGameObjectTool : McpToolBase
    {
        public DuplicateGameObjectTool()
        {
            Name = "duplicate_gameobject";
            Description = "Duplicates a GameObject (and its children) in the scene by path or instance ID";
        }

        /// <summary>
        /// Execute the DuplicateGameObject tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject</param>
        public override JObject Execute(JObject parameters)
        {
            string objectPath = parameters["objectPath"]?.ToObject<string>();
            int? instanceId = parameters["instanceId"]?.ToObject<int?>();
            string newName = parameters["newName"]?.ToObject<string>();

            // Validate parameters - require either objectPath or instanceId
            if (string.IsNullOrEmpty(objectPath) && !instanceId.HasValue)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'objectPath' or 'instanceId' not provided",
                    "validation_error"
                );
            }

            GameObject sourceGameObject = instanceId.HasValue
                ? EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject
                : GameObjectPathResolver.FindByPath(objectPath);

            if (sourceGameObject == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"GameObject not found" + (instanceId.HasValue ? $" with instance ID: {instanceId.Value}" : $": {objectPath}"),
                    "not_found_error"
                );
            }

            // Refuse to duplicate persistent project assets (e.g. a prefab asset's own instance ID) -
            // this tool only operates on scene objects.
            if (EditorUtility.IsPersistent(sourceGameObject))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Cannot duplicate '{sourceGameObject.name}': it is a persistent project asset, not a scene object. " +
                    "Use 'add_asset_to_scene' to instantiate a prefab asset into the scene instead.",
                    "validation_error"
                );
            }

            // Preserve the prefab connection when duplicating a prefab instance, matching AddAssetToSceneTool's
            // approach - but only for the outermost root of the prefab instance. PrefabUtility.InstantiatePrefab
            // always instantiates the prefab asset's root, so calling it with a non-root member's corresponding
            // object would incorrectly create a whole new prefab instance instead of duplicating just that member.
            // Non-root members fall back to a plain Object.Instantiate below.
            GameObject duplicatedGameObject;
            bool isPrefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(sourceGameObject) == sourceGameObject;
            PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(sourceGameObject);
            Object prefabSource = isPrefabInstanceRoot && prefabType != PrefabAssetType.NotAPrefab
                ? PrefabUtility.GetCorrespondingObjectFromSource(sourceGameObject)
                : null;
            if (prefabSource != null)
            {
                duplicatedGameObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabSource, sourceGameObject.transform.parent);
                duplicatedGameObject.transform.SetSiblingIndex(sourceGameObject.transform.GetSiblingIndex() + 1);
                duplicatedGameObject.transform.localPosition = sourceGameObject.transform.localPosition;
                duplicatedGameObject.transform.localRotation = sourceGameObject.transform.localRotation;
                duplicatedGameObject.transform.localScale = sourceGameObject.transform.localScale;

                // Carry over the source instance's property overrides (e.g. modified serialized field
                // values) so the duplicate isn't silently reset to the prefab's default state. This does
                // NOT cover structural overrides (added/removed components, added/removed child objects,
                // or nested-prefab-specific overrides) - those would need PrefabUtility.GetObjectOverrides /
                // GetAddedComponents / GetAddedGameObjects, which is a larger change left for a follow-up.
                // It also does NOT remap object-reference overrides that point at the source instance's own
                // sub-objects (e.g. a field overridden to reference one of its own children) - those would
                // still point at the original instance's sub-object rather than the duplicate's counterpart,
                // which needs a source-to-duplicate hierarchy mapping to fix correctly. Left as a known gap
                // alongside the structural-override limitation above rather than attempting an unvalidated
                // remapping without a live Editor to test against.
                PropertyModification[] overrides = PrefabUtility.GetPropertyModifications(sourceGameObject);
                if (overrides != null && overrides.Length > 0)
                {
                    PrefabUtility.SetPropertyModifications(duplicatedGameObject, overrides);
                }
            }
            else
            {
                duplicatedGameObject = Object.Instantiate(sourceGameObject, sourceGameObject.transform.parent);
            }

            // Instantiate(..., parent: null) places the new object in the active scene, not necessarily
            // the source's scene. For a root object (no parent) being duplicated in a loaded-but-inactive
            // scene, move it back into the source's own scene so the duplicate doesn't silently end up
            // somewhere else.
            if (sourceGameObject.transform.parent == null && duplicatedGameObject.scene != sourceGameObject.scene)
            {
                SceneManager.MoveGameObjectToScene(duplicatedGameObject, sourceGameObject.scene);
            }

            if (!string.IsNullOrEmpty(newName))
            {
                duplicatedGameObject.name = newName;
            }
            else if (sourceGameObject.transform.parent != null)
            {
                duplicatedGameObject.name = GameObjectUtility.GetUniqueNameForSibling(sourceGameObject.transform.parent, sourceGameObject.name);
            }
            else
            {
                // GetUniqueNameForSibling(null, ...) checks root objects in the *active* scene, which is
                // wrong when the source (and now the duplicate, per the scene-move above) is a root object
                // in a different loaded scene. Check uniqueness against the duplicate's actual scene instead.
                duplicatedGameObject.name = GetUniqueRootName(duplicatedGameObject.scene, sourceGameObject.name);
            }

            Undo.RegisterCreatedObjectUndo(duplicatedGameObject, "Duplicate GameObject");

            McpLogger.LogInfo($"[MCP Unity] Duplicated GameObject '{sourceGameObject.name}' as '{duplicatedGameObject.name}' (instance ID {duplicatedGameObject.GetInstanceID()})");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully duplicated GameObject '{sourceGameObject.name}' as '{duplicatedGameObject.name}'",
                ["instanceId"] = duplicatedGameObject.GetInstanceID(),
                ["name"] = duplicatedGameObject.name,
                ["sourceInstanceId"] = sourceGameObject.GetInstanceID()
            };
        }

        /// <summary>
        /// Finds a unique name for a new root GameObject in the given scene, based on the given base name,
        /// checking against that scene's existing root object names (not necessarily the active scene).
        /// </summary>
        /// <param name="scene">The scene the new root object will belong to</param>
        /// <param name="baseName">The name to make unique</param>
        /// <returns>The base name if already unique, otherwise the base name with a " (n)" suffix</returns>
        private static string GetUniqueRootName(Scene scene, string baseName)
        {
            HashSet<string> existingNames = new HashSet<string>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                existingNames.Add(root.name);
            }

            if (!existingNames.Contains(baseName))
            {
                return baseName;
            }

            int suffix = 1;
            string candidate;
            do
            {
                candidate = $"{baseName} ({suffix})";
                suffix++;
            } while (existingNames.Contains(candidate));

            return candidate;
        }
    }
}
