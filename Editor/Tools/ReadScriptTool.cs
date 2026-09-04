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

            if (!path.StartsWith("Assets/") || !path.EndsWith(".cs"))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Invalid script path '{path}'. Path must start with 'Assets/' and end with '.cs'.",
                    "validation_error"
                );
            }

            if (!File.Exists(path))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"No script found at '{path}'.",
                    "not_found_error"
                );
            }

            string content = File.ReadAllText(path);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully read script '{path}'",
                ["path"] = path,
                ["content"] = content
            };
        }
    }
}
