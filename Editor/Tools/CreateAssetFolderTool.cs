using UnityEditor;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for creating a new folder in the Unity Asset Database
    /// </summary>
    public class CreateAssetFolderTool : McpToolBase
    {
        public CreateAssetFolderTool()
        {
            Name = "create_asset_folder";
            Description = "Creates a new folder at the given parent path in the Unity project";
        }

        /// <summary>
        /// Execute the CreateAssetFolder tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject. Expects 'parentPath' and 'folderName'</param>
        public override JObject Execute(JObject parameters)
        {
            string parentPath = parameters["parentPath"]?.ToObject<string>();
            string folderName = parameters["folderName"]?.ToObject<string>();

            if (string.IsNullOrEmpty(parentPath) || string.IsNullOrEmpty(folderName))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameters 'parentPath' and 'folderName' must both be provided",
                    "validation_error"
                );
            }

            if (!parentPath.StartsWith("Assets") )
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "'parentPath' must start with 'Assets'",
                    "validation_error"
                );
            }

            if (!AssetDatabase.IsValidFolder(parentPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"'{parentPath}' is not an existing folder in the project.",
                    "not_found_error"
                );
            }

            string newFolderPath = $"{parentPath.TrimEnd('/')}/{folderName}";
            if (AssetDatabase.IsValidFolder(newFolderPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"A folder already exists at '{newFolderPath}'.",
                    "validation_error"
                );
            }

            string newFolderGuid = AssetDatabase.CreateFolder(parentPath, folderName);
            if (string.IsNullOrEmpty(newFolderGuid))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to create folder '{newFolderPath}'.",
                    "asset_error"
                );
            }

            McpLogger.LogInfo($"Created folder '{newFolderPath}'");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully created folder '{newFolderPath}'",
                ["path"] = newFolderPath,
                ["guid"] = newFolderGuid
            };
        }
    }
}
