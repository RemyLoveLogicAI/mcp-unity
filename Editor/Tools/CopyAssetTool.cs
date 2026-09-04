using UnityEditor;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for copying an asset to a new path in the Unity Asset Database
    /// </summary>
    public class CopyAssetTool : McpToolBase
    {
        public CopyAssetTool()
        {
            Name = "copy_asset";
            Description = "Copies an asset from one path to another in the Unity project";
        }

        /// <summary>
        /// Execute the CopyAsset tool with the provided parameters synchronously
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

            if (!AssetDatabase.CopyAsset(sourcePath, destinationPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to copy asset from '{sourcePath}' to '{destinationPath}'. The destination may already exist or the path may be invalid.",
                    "asset_error"
                );
            }

            McpLogger.LogInfo($"Copied asset '{sourcePath}' to '{destinationPath}'");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully copied asset to '{destinationPath}'",
                ["sourcePath"] = sourcePath,
                ["destinationPath"] = destinationPath
            };
        }
    }
}
