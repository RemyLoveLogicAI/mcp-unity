using System.Collections.Generic;
using UnityEditor;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for deleting one or more assets from the Unity project
    /// </summary>
    public class DeleteAssetTool : McpToolBase
    {
        public DeleteAssetTool()
        {
            Name = "delete_asset";
            Description = "Deletes one or more assets by path. Moves them to the OS trash (recoverable) rather " +
                "than permanently deleting, via AssetDatabase.MoveAssetToTrash.";
        }

        /// <summary>
        /// Execute the DeleteAsset tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject. Expects 'paths' (array of strings)</param>
        public override JObject Execute(JObject parameters)
        {
            JArray pathsArray = parameters["paths"] as JArray;

            if (pathsArray == null || pathsArray.Count == 0)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'paths' not provided or empty. Expected a non-empty array of asset paths.",
                    "validation_error"
                );
            }

            List<string> deleted = new List<string>();
            List<string> failed = new List<string>();

            foreach (JToken pathToken in pathsArray)
            {
                string path = pathToken?.ToObject<string>();

                if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/"))
                {
                    failed.Add($"{path ?? "(null)"} (must start with 'Assets/')");
                    continue;
                }

                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) == null)
                {
                    failed.Add($"{path} (not found)");
                    continue;
                }

                if (AssetDatabase.MoveAssetToTrash(path))
                {
                    deleted.Add(path);
                }
                else
                {
                    failed.Add($"{path} (delete failed)");
                }
            }

            McpLogger.LogInfo($"[MCP Unity] Deleted {deleted.Count} asset(s), {failed.Count} failed");

            return new JObject
            {
                ["success"] = deleted.Count > 0 && failed.Count == 0,
                ["type"] = "text",
                ["message"] = failed.Count == 0
                    ? $"Successfully deleted {deleted.Count} asset(s)"
                    : $"Deleted {deleted.Count} asset(s), {failed.Count} failed: {string.Join(", ", failed)}",
                ["deletedPaths"] = new JArray(deleted),
                ["failedPaths"] = new JArray(failed)
            };
        }
    }
}
