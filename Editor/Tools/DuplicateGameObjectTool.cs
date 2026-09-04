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
                : GameObject.Find(objectPath);

            if (sourceGameObject == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"GameObject not found" + (instanceId.HasValue ? $" with instance ID: {instanceId.Value}" : $": {objectPath}"),
                    "not_found_error"
                );
            }

            // Preserve the prefab connection when duplicating a prefab instance, matching AddAssetToSceneTool's approach
            GameObject duplicatedGameObject;
            PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(sourceGameObject);
            if (prefabType != PrefabAssetType.NotAPrefab)
            {
                Object prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(sourceGameObject);
                duplicatedGameObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabSource, sourceGameObject.transform.parent);
                duplicatedGameObject.transform.SetSiblingIndex(sourceGameObject.transform.GetSiblingIndex() + 1);
                duplicatedGameObject.transform.localPosition = sourceGameObject.transform.localPosition;
                duplicatedGameObject.transform.localRotation = sourceGameObject.transform.localRotation;
                duplicatedGameObject.transform.localScale = sourceGameObject.transform.localScale;
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
