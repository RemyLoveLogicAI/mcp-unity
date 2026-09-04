using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for saving the active scene, optionally to a new path, or all open scenes at once
    /// </summary>
    public class SaveSceneTool : McpToolBase
    {
        public SaveSceneTool()
        {
            Name = "save_scene";
            Description = "Saves the active scene (optionally to a new path via 'path'), or all open scenes in " +
                "place when 'saveAll' is true";
        }

        /// <summary>
        /// Execute the SaveScene tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject. Expects optional 'path' and 'saveAll'</param>
        public override JObject Execute(JObject parameters)
        {
            bool saveAll = parameters["saveAll"]?.ToObject<bool>() ?? false;
            string path = parameters["path"]?.ToObject<string>();

            if (saveAll)
            {
                if (!EditorSceneManager.SaveOpenScenes())
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        "One or more open scenes failed to save.",
                        "scene_error"
                    );
                }

                McpLogger.LogInfo("[MCP Unity] Saved all open scenes");

                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = "Successfully saved all open scenes"
                };
            }

            if (!string.IsNullOrEmpty(path) && (!path.StartsWith("Assets/") || !path.EndsWith(".unity")))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Invalid scene path '{path}'. Path must start with 'Assets/' and end with '.unity'.",
                    "validation_error"
                );
            }

            Scene activeScene = SceneManager.GetActiveScene();
            bool saved = string.IsNullOrEmpty(path)
                ? EditorSceneManager.SaveScene(activeScene)
                : EditorSceneManager.SaveScene(activeScene, path);

            if (!saved)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to save scene '{activeScene.name}'.",
                    "scene_error"
                );
            }

            McpLogger.LogInfo($"[MCP Unity] Saved scene '{activeScene.path}'");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully saved scene '{activeScene.path}'",
                ["path"] = activeScene.path,
                ["name"] = activeScene.name
            };
        }
    }
}
