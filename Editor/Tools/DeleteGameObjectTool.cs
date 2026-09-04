using UnityEngine;
using UnityEditor;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for deleting GameObjects from the Unity scene by path or instance ID
    /// </summary>
    public class DeleteGameObjectTool : McpToolBase
    {
        public DeleteGameObjectTool()
        {
            Name = "delete_gameobject";
            Description = "Deletes a GameObject (and its children) from the scene by path or instance ID";
        }

        /// <summary>
        /// Execute the DeleteGameObject tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject</param>
        public override JObject Execute(JObject parameters)
        {
            string objectPath = parameters["objectPath"]?.ToObject<string>();
            int? instanceId = parameters["instanceId"]?.ToObject<int?>();

            // Validate parameters - require either objectPath or instanceId
            if (string.IsNullOrEmpty(objectPath) && !instanceId.HasValue)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'objectPath' or 'instanceId' not provided",
                    "validation_error"
                );
            }

            GameObject targetGameObject = instanceId.HasValue
                ? EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject
                : GameObject.Find(objectPath);

            if (targetGameObject == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"GameObject not found" + (instanceId.HasValue ? $" with instance ID: {instanceId.Value}" : $": {objectPath}"),
                    "not_found_error"
                );
            }

            string deletedName = targetGameObject.name;
            int deletedInstanceId = targetGameObject.GetInstanceID();

            Undo.DestroyObjectImmediate(targetGameObject);

            McpLogger.LogInfo($"[MCP Unity] Deleted GameObject: '{deletedName}' (instance ID {deletedInstanceId})");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully deleted GameObject '{deletedName}'",
                ["instanceId"] = deletedInstanceId,
                ["name"] = deletedName
            };
        }
    }
}
