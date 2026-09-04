using System;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for opening a scene from the project, replacing or adding to the currently loaded scenes
    /// </summary>
    public class OpenSceneTool : McpToolBase
    {
        public OpenSceneTool()
        {
            Name = "open_scene";
            Description = "Opens a scene from the given Assets path. By default (additive=false) this replaces all " +
                "currently loaded scenes and discards their unsaved changes without prompting - save first with " +
                "'save_scene' if needed.";
        }

        /// <summary>
        /// Execute the OpenScene tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject. Expects 'path' and optional 'additive'</param>
        public override JObject Execute(JObject parameters)
        {
            string path = parameters["path"]?.ToObject<string>();
            bool additive = parameters["additive"]?.ToObject<bool>() ?? false;

            if (string.IsNullOrEmpty(path))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'path' not provided",
                    "validation_error"
                );
            }

            if (!File.Exists(path))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"No scene file found at '{path}'.",
                    "not_found_error"
                );
            }

            Scene scene;
            try
            {
                scene = EditorSceneManager.OpenScene(path, additive ? OpenSceneMode.Additive : OpenSceneMode.Single);
            }
            catch (Exception ex)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to open scene '{path}': {ex.Message}",
                    "scene_error"
                );
            }

            McpLogger.LogInfo($"[MCP Unity] Opened scene '{path}' ({(additive ? "additive" : "single")})");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully opened scene '{scene.path}'",
                ["path"] = scene.path,
                ["name"] = scene.name,
                ["isLoaded"] = scene.isLoaded
            };
        }
    }
}
