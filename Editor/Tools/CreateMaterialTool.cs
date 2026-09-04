using UnityEngine;
using UnityEditor;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for creating a new material asset with a given shader
    /// </summary>
    public class CreateMaterialTool : McpToolBase
    {
        private const string DefaultShaderName = "Standard";

        public CreateMaterialTool()
        {
            Name = "create_material";
            Description = "Creates a new material asset at the given path, using the named shader (defaults to 'Standard' if not found)";
        }

        /// <summary>
        /// Execute the CreateMaterial tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject. Expects 'path' and optional 'shaderName'</param>
        public override JObject Execute(JObject parameters)
        {
            string path = parameters["path"]?.ToObject<string>();
            string shaderName = parameters["shaderName"]?.ToObject<string>();

            if (string.IsNullOrEmpty(path))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'path' not provided",
                    "validation_error"
                );
            }

            if (!path.StartsWith("Assets/") || !path.EndsWith(".mat"))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Invalid material path '{path}'. Path must start with 'Assets/' and end with '.mat'.",
                    "validation_error"
                );
            }

            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"A material already exists at '{path}'.",
                    "validation_error"
                );
            }

            Shader shader = string.IsNullOrEmpty(shaderName) ? null : Shader.Find(shaderName);
            string resolvedShaderName = shaderName;
            if (shader == null)
            {
                shader = Shader.Find(DefaultShaderName);
                resolvedShaderName = DefaultShaderName;
            }

            if (shader == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Could not find shader '{shaderName ?? DefaultShaderName}' to create the material.",
                    "not_found_error"
                );
            }

            Material material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);

            McpLogger.LogInfo($"[MCP Unity] Created material '{path}' with shader '{resolvedShaderName}'");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully created material '{path}' with shader '{resolvedShaderName}'",
                ["path"] = path,
                ["shaderName"] = resolvedShaderName
            };
        }
    }
}
