using UnityEngine.SceneManagement;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for setting which currently loaded scene is the active scene
    /// </summary>
    public class SetActiveSceneTool : McpToolBase
    {
        public SetActiveSceneTool()
        {
            Name = "set_active_scene";
            Description = "Sets which currently loaded scene is the active scene, by scene path";
        }

        /// <summary>
        /// Execute the SetActiveScene tool with the provided parameters synchronously
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

            if (!TryFindLoadedScene(path, out Scene targetScene))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"No currently loaded scene found with path '{path}'. Open it first with 'open_scene' (additive: true).",
                    "not_found_error"
                );
            }

            if (!SceneManager.SetActiveScene(targetScene))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to set '{path}' as the active scene.",
                    "scene_error"
                );
            }

            McpLogger.LogInfo($"[MCP Unity] Set active scene to '{path}'");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully set '{path}' as the active scene",
                ["path"] = targetScene.path,
                ["name"] = targetScene.name
            };
        }

        /// <summary>
        /// Finds a currently loaded scene by its asset path
        /// </summary>
        private static bool TryFindLoadedScene(string path, out Scene scene)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene candidate = SceneManager.GetSceneAt(i);
                if (candidate.path == path)
                {
                    scene = candidate;
                    return true;
                }
            }

            scene = default;
            return false;
        }
    }
}
