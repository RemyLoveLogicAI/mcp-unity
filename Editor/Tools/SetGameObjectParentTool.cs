using UnityEngine;
using UnityEditor;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for changing a GameObject's parent in the hierarchy
    /// </summary>
    public class SetGameObjectParentTool : McpToolBase
    {
        public SetGameObjectParentTool()
        {
            Name = "set_gameobject_parent";
            Description = "Sets a GameObject's parent in the hierarchy, or moves it to the scene root when " +
                "'parentInstanceId'/'parentObjectPath' are omitted";
        }

        /// <summary>
        /// Execute the SetGameObjectParent tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject. Expects 'instanceId'/'objectPath' for the
        /// target, optional 'parentInstanceId'/'parentObjectPath' for the new parent, and optional
        /// 'worldPositionStays'</param>
        public override JObject Execute(JObject parameters)
        {
            int? instanceId = parameters["instanceId"]?.ToObject<int?>();
            string objectPath = parameters["objectPath"]?.ToObject<string>();

            if (!instanceId.HasValue && string.IsNullOrEmpty(objectPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Either 'instanceId' or 'objectPath' must be provided to identify the GameObject to reparent",
                    "validation_error"
                );
            }

            GameObject target = instanceId.HasValue
                ? EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject
                : GameObject.Find(objectPath) ?? FindGameObjectByPath(objectPath);

            if (target == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"GameObject not found" + (instanceId.HasValue ? $" with instance ID: {instanceId.Value}" : $": {objectPath}"),
                    "not_found_error"
                );
            }

            if (EditorUtility.IsPersistent(target))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Cannot reparent '{target.name}': it is a persistent project asset, not a scene object.",
                    "validation_error"
                );
            }

            int? parentInstanceId = parameters["parentInstanceId"]?.ToObject<int?>();
            string parentObjectPath = parameters["parentObjectPath"]?.ToObject<string>();
            bool worldPositionStays = parameters["worldPositionStays"]?.ToObject<bool>() ?? true;

            Transform newParent = null;
            if (parentInstanceId.HasValue || !string.IsNullOrEmpty(parentObjectPath))
            {
                GameObject parentObject = parentInstanceId.HasValue
                    ? EditorUtility.InstanceIDToObject(parentInstanceId.Value) as GameObject
                    : GameObject.Find(parentObjectPath) ?? FindGameObjectByPath(parentObjectPath);

                if (parentObject == null)
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        $"Parent GameObject not found" + (parentInstanceId.HasValue ? $" with instance ID: {parentInstanceId.Value}" : $": {parentObjectPath}"),
                        "not_found_error"
                    );
                }

                if (EditorUtility.IsPersistent(parentObject))
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        $"Cannot reparent '{target.name}' to '{parentObject.name}': it is a persistent project asset, not a scene object.",
                        "validation_error"
                    );
                }

                if (parentObject == target || parentObject.transform.IsChildOf(target.transform))
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        $"Cannot set '{parentObject.name}' as the parent of '{target.name}': it is the same object or one of its own descendants.",
                        "validation_error"
                    );
                }

                newParent = parentObject.transform;
            }

            Undo.SetTransformParent(target.transform, newParent, worldPositionStays, "Set GameObject Parent");
            EditorUtility.SetDirty(target);

            string newParentName = newParent != null ? newParent.name : "scene root";
            McpLogger.LogInfo($"[MCP Unity] Set parent of '{target.name}' to '{newParentName}'");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully set parent of '{target.name}' to '{newParentName}'",
                ["instanceId"] = target.GetInstanceID(),
                ["name"] = target.name
            };
        }

        /// <summary>
        /// Find a GameObject by its hierarchy path, including inactive objects
        /// </summary>
        /// <param name="path">The path to the GameObject (e.g. "Canvas/Panel/Button")</param>
        /// <returns>The GameObject if found, null otherwise</returns>
        private GameObject FindGameObjectByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string[] pathParts = path.Split('/');
            GameObject[] rootGameObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (GameObject rootObj in rootGameObjects)
            {
                if (rootObj.name != pathParts[0])
                {
                    continue;
                }

                GameObject current = rootObj;
                for (int i = 1; i < pathParts.Length; i++)
                {
                    Transform child = current.transform.Find(pathParts[i]);
                    if (child == null)
                    {
                        current = null;
                        break;
                    }

                    current = child.gameObject;
                }

                if (current != null)
                {
                    return current;
                }
            }

            return null;
        }
    }
}
