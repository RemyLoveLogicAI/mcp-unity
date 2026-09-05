using UnityEditor;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for deleting a C# script file
    /// </summary>
    public class DeleteScriptTool : McpToolBase
    {
        public DeleteScriptTool()
        {
            Name = "delete_script";
            Description = "Deletes a C# script file by path. Moves it to the OS trash (recoverable) rather " +
                "than permanently deleting, via AssetDatabase.MoveAssetToTrash.";
        }

        /// <summary>
        /// Execute the DeleteScript tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject. Expects 'path'</param>
        public override JObject Execute(JObject parameters)
        {
            string path = parameters["path"]?.ToObject<string>();

            if (string.IsNullOrEmpty(path))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'path' not provided",
                    "validation_error"
                );
            }

            if (!path.StartsWith("Assets/") || !path.EndsWith(".cs"))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Invalid script path '{path}'. Path must start with 'Assets/' and end with '.cs'.",
                    "validation_error"
                );
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"No script found at '{path}'.",
                    "not_found_error"
                );
            }

            if (!AssetDatabase.MoveAssetToTrash(path))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to delete script '{path}'.",
                    "delete_error"
                );
            }

            McpLogger.LogInfo($"Deleted script '{path}'");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully deleted script '{path}'"
            };
        }
    }
}
