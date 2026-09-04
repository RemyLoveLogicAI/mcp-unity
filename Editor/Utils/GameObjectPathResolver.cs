using UnityEngine;
using UnityEngine.SceneManagement;

namespace McpUnity.Utils
{
    /// <summary>
    /// Resolves a hierarchy path (e.g. "Parent/Child/Grandchild") to a GameObject across all loaded
    /// scenes, including inactive objects. Unlike GameObject.Find, which only searches active objects,
    /// this walks scene root objects (active or not) and then Transform.Find for the remaining segments.
    /// </summary>
    public static class GameObjectPathResolver
    {
        public static GameObject FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            path = path.Trim('/');
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string[] parts = path.Split('/');
            Transform current = FindRoot(parts[0]);

            for (int i = 1; current != null && i < parts.Length; i++)
            {
                current = current.Find(parts[i]);
            }

            return current != null ? current.gameObject : null;
        }

        private static Transform FindRoot(string rootName)
        {
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (GameObject rootObject in scene.GetRootGameObjects())
                {
                    if (rootObject.name == rootName)
                    {
                        return rootObject.transform;
                    }
                }
            }

            return null;
        }
    }
}
