using System.IO;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for creating a new empty scene and saving it to the project
    /// </summary>
    public class CreateSceneTool : McpToolBase
    {
        public CreateSceneTool()
        {
            Name = "create_scene";
            Description = "Creates a new empty scene with default GameObjects and saves it to the given Assets path. " +
                "By default (loadAdditively=false) this replaces all currently loaded scenes and discards their " +
                "unsaved changes without prompting - save first with 'save_scene' or pass loadAdditively: true.";
        }

        /// <summary>
        /// Execute the CreateScene tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject. Expects 'path' and optional 'loadAdditively'</param>
        public override JObject Execute(JObject parameters)
        {
            string path = parameters["path"]?.ToObject<string>();
            bool loadAdditively = parameters["loadAdditively"]?.ToObject<bool>() ?? false;

            if (string.IsNullOrEmpty(path))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'path' not provided",
                    "validation_error"
                );
            }

            if (!path.StartsWith("Assets/") || !path.EndsWith(".unity"))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Invalid scene path '{path}'. Path must start with 'Assets/' and end with '.unity'.",
                    "validation_error"
                );
            }

            if (File.Exists(path))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"A scene already exists at '{path}'. Choose a different path or delete the existing scene first.",
                    "validation_error"
                );
            }

            // Ensure the target directory exists BEFORE NewScene(Single) discards the current scenes,
            // otherwise a valid-looking path into a non-existent folder makes SaveScene fail and leaves
            // the user with no old scenes and no saved new scene.
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Scene newScene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects,
                loadAdditively ? NewSceneMode.Additive : NewSceneMode.Single
            );

            if (!EditorSceneManager.SaveScene(newScene, path))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to save new scene to '{path}'.",
                    "scene_error"
                );
            }

            McpLogger.LogInfo($"[MCP Unity] Created scene '{path}'");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully created scene '{path}'",
                ["path"] = newScene.path,
                ["name"] = newScene.name
            };
        }
    }
}
