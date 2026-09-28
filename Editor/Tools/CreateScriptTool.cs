using System;
using System.IO;
using UnityEditor;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for creating a new C# script file
    /// </summary>
    public class CreateScriptTool : McpToolBase
    {
        public CreateScriptTool()
        {
            Name = "create_script";
            Description = "Creates a new C# script file with the given contents at the given Assets path";
        }

        /// <summary>
        /// Execute the CreateScript tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject. Expects 'path' and 'content'</param>
        public override JObject Execute(JObject parameters)
        {
            string path = parameters["path"]?.ToObject<string>();
            string content = parameters["content"]?.ToObject<string>();

            if (string.IsNullOrEmpty(path))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'path' not provided",
                    "validation_error"
                );
            }

            if (content == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'content' not provided",
                    "validation_error"
                );
            }

            if (!path.StartsWith("Assets/") || !path.EndsWith(".cs") || !TryResolveWithinAssets(path, out string fullPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Invalid script path '{path}'. Path must start with 'Assets/', end with '.cs', and stay within the project's Assets folder.",
                    "validation_error"
                );
            }

            if (File.Exists(fullPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"A script already exists at '{path}'. Choose a different path or delete the existing script first.",
                    "validation_error"
                );
            }

            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, content);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            McpLogger.LogInfo($"Created script '{path}'");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully created script '{path}'. Unity will compile it shortly.",
                ["path"] = path
            };
        }

        /// <summary>
        /// Resolve a validated "Assets/..." path to its full filesystem path, rejecting any path
        /// (e.g. via "../" segments) that would resolve outside the project's Assets folder
        /// </summary>
        /// <param name="path">The "Assets/..." relative path to resolve</param>
        /// <param name="fullPath">The resolved full path, if it stays within Assets</param>
        /// <returns>True if the path resolves within the Assets folder</returns>
        private static bool TryResolveWithinAssets(string path, out string fullPath)
        {
            fullPath = null;

            try
            {
                string assetsRoot = Path.GetFullPath("Assets") + Path.DirectorySeparatorChar;
                string resolved = Path.GetFullPath(path);

                if (!resolved.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                fullPath = resolved;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
