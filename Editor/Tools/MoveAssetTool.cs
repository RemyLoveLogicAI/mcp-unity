using UnityEditor;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for moving (or renaming) an asset to a new path in the Unity Asset Database
    /// </summary>
    public class MoveAssetTool : McpToolBase
    {
        public MoveAssetTool()
        {
            Name = "move_asset";
            Description = "Moves or renames an asset from one path to another in the Unity project";
        }

        /// <summary>
        /// Execute the MoveAsset tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject. Expects 'sourcePath' and 'destinationPath'</param>
        public override JObject Execute(JObject parameters)
        {
            string sourcePath = parameters["sourcePath"]?.ToObject<string>();
            string destinationPath = parameters["destinationPath"]?.ToObject<string>();

            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destinationPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameters 'sourcePath' and 'destinationPath' must both be provided",
                    "validation_error"
                );
            }

            if (!sourcePath.StartsWith("Assets/") || !destinationPath.StartsWith("Assets/"))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Both 'sourcePath' and 'destinationPath' must start with 'Assets/'",
                    "validation_error"
                );
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(sourcePath) == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"No asset found at '{sourcePath}'",
                    "not_found_error"
                );
            }

            // AssetDatabase.MoveAsset returns an empty string on success, or an error message on failure
            string error = AssetDatabase.MoveAsset(sourcePath, destinationPath);
            if (!string.IsNullOrEmpty(error))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to move asset from '{sourcePath}' to '{destinationPath}': {error}",
                    "asset_error"
                );
            }

            McpLogger.LogInfo($"[MCP Unity] Moved asset '{sourcePath}' to '{destinationPath}'");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully moved asset to '{destinationPath}'",
                ["sourcePath"] = sourcePath,
                ["destinationPath"] = destinationPath
            };
        }
    }
}
