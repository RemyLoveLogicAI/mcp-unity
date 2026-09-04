using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;

namespace McpUnity.Resources
{
    /// <summary>
    /// Resource for retrieving the current state of the Unity Editor (play mode, pause, compilation)
    /// </summary>
    public class GetEditorStateResource : McpResourceBase
    {
        public GetEditorStateResource()
        {
            Name = "get_editor_state";
            Description = "Retrieves the current state of the Unity Editor (playmode, paused, compiling)";
            Uri = "unity://editor-state";
        }

        /// <summary>
        /// Fetch the current Unity Editor application state
        /// </summary>
        /// <param name="parameters">Unused for this resource</param>
        /// <returns>A JObject containing the Editor state</returns>
        public override JObject Fetch(JObject parameters)
        {
            return new JObject
            {
                ["success"] = true,
                ["message"] = "Retrieved Unity Editor state",
                ["isPlaying"] = EditorApplication.isPlaying,
                ["isPaused"] = EditorApplication.isPaused,
                ["isCompiling"] = EditorApplication.isCompiling,
                ["applicationPath"] = EditorApplication.applicationPath,
                ["unityVersion"] = Application.unityVersion
            };
        }
    }
}
