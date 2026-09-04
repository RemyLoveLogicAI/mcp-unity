using System;
using System.IO;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for reading the contents of a C# script file
    /// </summary>
    public class ReadScriptTool : McpToolBase
    {
        public ReadScriptTool()
        {
            Name = "read_script";
            Description = "Reads the contents of a C# script file from the project";
        }

        /// <summary>
        /// Execute the ReadScript tool with the provided parameters synchronously
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

            if (!path.StartsWith("Assets/") || !path.EndsWith(".cs") || !TryResolveWithinAssets(path, out string fullPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Invalid script path '{path}'. Path must start with 'Assets/', end with '.cs', and stay within the project's Assets folder.",
                    "validation_error"
                );
            }

            if (!File.Exists(fullPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"No script found at '{path}'.",
                    "not_found_error"
                );
            }

            string content = File.ReadAllText(fullPath);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully read script '{path}'",
                ["path"] = path,
                ["content"] = content
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
