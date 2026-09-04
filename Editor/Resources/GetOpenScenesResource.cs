using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;

namespace McpUnity.Resources
{
    /// <summary>
    /// Resource for retrieving the list of currently loaded/open scenes
    /// </summary>
    public class GetOpenScenesResource : McpResourceBase
    {
        public GetOpenScenesResource()
        {
            Name = "get_open_scenes";
            Description = "Retrieves the list of currently loaded scenes, including which one is active";
            Uri = "unity://scenes";
        }

        /// <summary>
        /// Fetch the list of currently loaded scenes
        /// </summary>
        /// <param name="parameters">Unused for this resource</param>
        /// <returns>A JObject containing the loaded scenes</returns>
        public override JObject Fetch(JObject parameters)
        {
            JArray scenesArray = new JArray();
            Scene activeScene = SceneManager.GetActiveScene();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                scenesArray.Add(new JObject
                {
                    ["name"] = scene.name,
                    ["path"] = scene.path,
                    ["isLoaded"] = scene.isLoaded,
                    ["isDirty"] = scene.isDirty,
                    ["isActive"] = scene == activeScene,
                    ["rootCount"] = scene.rootCount
                });
            }

            return new JObject
            {
                ["success"] = true,
                ["message"] = $"Retrieved {scenesArray.Count} open scene(s)",
                ["scenes"] = scenesArray
            };
        }
    }
}
