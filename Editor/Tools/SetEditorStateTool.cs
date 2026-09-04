using UnityEditor;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for controlling the Unity Editor's play mode state (play/pause/resume/stop)
    /// </summary>
    public class SetEditorStateTool : McpToolBase
    {
        public SetEditorStateTool()
        {
            Name = "set_editor_state";
            Description = "Controls the Unity Editor's play mode state (play, pause, resume, or stop). " +
                "Play/stop transitions are asynchronous in Unity, so the isPlaying/isPaused values in the " +
                "response reflect the transition as requested and may not be fully applied yet; " +
                "poll the unity://editor-state resource to confirm the observed state.";
        }

        /// <summary>
        /// Execute the SetEditorState tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject. Expects 'state' to be one of 'play', 'pause', 'resume' or 'stop'</param>
        public override JObject Execute(JObject parameters)
        {
            string state = parameters["state"]?.ToObject<string>()?.ToLowerInvariant();

            if (string.IsNullOrEmpty(state))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'state' not provided. Expected one of: 'play', 'pause', 'resume', 'stop'",
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
                case "resume":
                    if (!EditorApplication.isPlaying)
                    {
                        return McpUnitySocketHandler.CreateErrorResponse(
                            "Cannot resume: the Editor is not in play mode.",
                            "invalid_state_error"
                        );
                    }
                    EditorApplication.isPaused = false;
                    break;
                case "stop":
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.isPlaying = false;
                    }
                    break;
                default:
                    return McpUnitySocketHandler.CreateErrorResponse(
                        $"Invalid 'state' value: '{state}'. Expected one of: 'play', 'pause', 'resume', 'stop'",
                        "validation_error"
                    );
            }

            McpLogger.LogInfo($"[MCP Unity] Requested editor state: {state}");

            // NOTE: EditorApplication.isPlaying/isPaused transitions are asynchronous (Unity applies them on a
            // later editor frame, typically after a domain reload for play/stop). The values below reflect the
            // state immediately after the request and may not yet match Unity's fully-applied state.
            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully requested editor state '{state}'",
                ["isPlaying"] = EditorApplication.isPlaying,
                ["isPaused"] = EditorApplication.isPaused,
                ["isCompiling"] = EditorApplication.isCompiling
            };
        }
    }
}
