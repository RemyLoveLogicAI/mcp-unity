using UnityEngine;
using UnityEditor;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for removing a component from a GameObject
    /// </summary>
    public class DestroyComponentTool : McpToolBase
    {
        public DestroyComponentTool()
        {
            Name = "destroy_component";
            Description = "Removes a component from a GameObject, identified by path or instance ID and the component's type name";
        }

        /// <summary>
        /// Execute the DestroyComponent tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject. Expects 'instanceId'/'objectPath' and 'componentName'</param>
        public override JObject Execute(JObject parameters)
        {
            int? instanceId = parameters["instanceId"]?.ToObject<int?>();
            string objectPath = parameters["objectPath"]?.ToObject<string>();
            string componentName = parameters["componentName"]?.ToObject<string>();

            if (!instanceId.HasValue && string.IsNullOrEmpty(objectPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Either 'instanceId' or 'objectPath' must be provided",
                    "validation_error"
                );
            }

            if (string.IsNullOrEmpty(componentName))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'componentName' not provided",
                    "validation_error"
                );
            }

            GameObject gameObject = instanceId.HasValue
                ? EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject
                : GameObject.Find(objectPath);

            if (gameObject == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"GameObject not found" + (instanceId.HasValue ? $" with instance ID: {instanceId.Value}" : $": {objectPath}"),
                    "not_found_error"
                );
            }

            Component component = gameObject.GetComponent(componentName);
            if (component == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"GameObject '{gameObject.name}' does not have a component of type '{componentName}'",
                    "not_found_error"
                );
            }

            if (component is Transform)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Cannot destroy a GameObject's Transform component.",
                    "validation_error"
                );
            }

            Undo.DestroyObjectImmediate(component);
            EditorUtility.SetDirty(gameObject);

            McpLogger.LogInfo($"[MCP Unity] Destroyed component '{componentName}' on GameObject '{gameObject.name}'");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully removed component '{componentName}' from GameObject '{gameObject.name}'"
            };
        }
    }
}
