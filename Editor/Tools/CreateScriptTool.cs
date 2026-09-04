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

            if (!path.StartsWith("Assets/") || !path.EndsWith(".cs"))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Invalid script path '{path}'. Path must start with 'Assets/' and end with '.cs'.",
                    "validation_error"
                );
            }

            if (File.Exists(path))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"A script already exists at '{path}'. Choose a different path or delete the existing script first.",
                    "validation_error"
                );
            }

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, content);
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
    }
}
