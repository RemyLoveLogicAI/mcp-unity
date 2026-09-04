using UnityEngine;
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

            // Preserve the prefab connection when duplicating a prefab instance, matching AddAssetToSceneTool's approach
            GameObject duplicatedGameObject;
            PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(sourceGameObject);
            Object prefabSource = prefabType != PrefabAssetType.NotAPrefab
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

            duplicatedGameObject.name = !string.IsNullOrEmpty(newName)
                ? newName
                : GameObjectUtility.GetUniqueNameForSibling(sourceGameObject.transform.parent, sourceGameObject.name);

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
    }
}
