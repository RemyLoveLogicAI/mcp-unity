using UnityEditor;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for controlling the Unity Editor's play mode state (play/pause/stop)
    /// </summary>
    public class SetEditorStateTool : McpToolBase
    {
        public SetEditorStateTool()
        {
            Name = "set_editor_state";
            Description = "Controls the Unity Editor's play mode state (play, pause, or stop)";
        }

        /// <summary>
        /// Execute the SetEditorState tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject. Expects 'state' to be one of 'play', 'pause' or 'stop'</param>
        public override JObject Execute(JObject parameters)
        {
            string state = parameters["state"]?.ToObject<string>()?.ToLowerInvariant();

            if (string.IsNullOrEmpty(state))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'state' not provided. Expected one of: 'play', 'pause', 'stop'",
                    "validation_error"
                );
            }

            switch (state)
            {
                case "play":
                    if (!EditorApplication.isPlaying)
                    {
                        EditorApplication.isPlaying = true;
                    }
                    break;
                case "pause":
                    if (!EditorApplication.isPlaying)
                    {
                        return McpUnitySocketHandler.CreateErrorResponse(
                            "Cannot pause: the Editor is not in play mode.",
                            "invalid_state_error"
                        );
                    }
                    EditorApplication.isPaused = true;
                    break;
                case "stop":
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.isPlaying = false;
                    }
                    break;
                default:
                    return McpUnitySocketHandler.CreateErrorResponse(
                        $"Invalid 'state' value: '{state}'. Expected one of: 'play', 'pause', 'stop'",
                        "validation_error"
                    );
            }

            McpLogger.LogInfo($"[MCP Unity] Set editor state: {state}");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully set editor state to '{state}'",
                ["isPlaying"] = EditorApplication.isPlaying,
                ["isPaused"] = EditorApplication.isPaused,
                ["isCompiling"] = EditorApplication.isCompiling
            };
        }
    }
}
