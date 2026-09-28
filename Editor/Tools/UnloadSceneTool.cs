using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for closing/unloading an additively loaded scene
    /// </summary>
    public class UnloadSceneTool : McpToolBase
    {
        public UnloadSceneTool()
        {
            Name = "unload_scene";
            Description = "Closes/unloads a currently loaded scene by path. The scene must not be the only loaded scene.";
        }

        /// <summary>
        /// Execute the UnloadScene tool with the provided parameters synchronously
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

            Scene targetScene = default;
            bool found = false;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene candidate = SceneManager.GetSceneAt(i);
                if (candidate.path == path)
                {
                    targetScene = candidate;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"No currently loaded scene found with path '{path}'.",
                    "not_found_error"
                );
            }

            if (SceneManager.sceneCount <= 1)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Cannot unload the only loaded scene. Open another scene first.",
                    "invalid_state_error"
                );
            }

            if (!EditorSceneManager.CloseScene(targetScene, true))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to unload scene '{path}'.",
                    "scene_error"
                );
            }

            McpLogger.LogInfo($"[MCP Unity] Unloaded scene '{path}'");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully unloaded scene '{path}'"
            };
        }
    }
}
