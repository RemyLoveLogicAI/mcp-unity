using System;
using UnityEngine;
using UnityEditor;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for capturing a screenshot from a Unity camera - either the active Scene view camera, or a
    /// specific/main camera in the scene - so an MCP client can visually inspect the current state of a
    /// scene after making changes.
    /// </summary>
    public class CaptureScreenshotTool : McpToolBase
    {
        private const int MaxDimension = 4096;

        public CaptureScreenshotTool()
        {
            Name = "capture_screenshot";
            Description = "Captures a screenshot from the active Scene view camera, or from a specific or " +
                "main scene camera, and returns it as an image";
        }

        /// <summary>
        /// Execute the CaptureScreenshot tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject. Expects optional 'source' ('scene_view' or
        /// 'camera', default 'scene_view'), 'cameraPath'/'cameraInstanceId' (when source='camera'), 'width',
        /// 'height' (default 1024x768), and 'format' ('png' or 'jpg', default 'png')</param>
        public override JObject Execute(JObject parameters)
        {
            string source = parameters["source"]?.ToObject<string>()?.ToLowerInvariant() ?? "scene_view";
            int width = parameters["width"]?.ToObject<int?>() ?? 1024;
            int height = parameters["height"]?.ToObject<int?>() ?? 768;
            string format = parameters["format"]?.ToObject<string>()?.ToLowerInvariant() ?? "png";

            if (width <= 0 || width > MaxDimension || height <= 0 || height > MaxDimension)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"'width' and 'height' must be between 1 and {MaxDimension}",
                    "validation_error"
                );
            }

            if (format != "png" && format != "jpg")
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Invalid 'format' value: '{format}'. Expected 'png' or 'jpg'",
                    "validation_error"
                );
            }

            Camera camera;
            if (source == "scene_view")
            {
                if (SceneView.lastActiveSceneView == null)
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        "No active Scene view found. Open a Scene view in the Editor first.",
                        "not_found_error"
                    );
                }

                camera = SceneView.lastActiveSceneView.camera;
            }
            else if (source == "camera")
            {
                string cameraPath = parameters["cameraPath"]?.ToObject<string>();
                int? cameraInstanceId = parameters["cameraInstanceId"]?.ToObject<int?>();

                GameObject cameraGameObject = null;
                if (cameraInstanceId.HasValue)
                {
                    cameraGameObject = EditorUtility.InstanceIDToObject(cameraInstanceId.Value) as GameObject;
                }
                else if (!string.IsNullOrEmpty(cameraPath))
                {
                    cameraGameObject = GameObject.Find(cameraPath);
                }

                camera = cameraGameObject != null ? cameraGameObject.GetComponent<Camera>() : Camera.main;

                if (camera == null)
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        cameraGameObject != null
                            ? $"GameObject '{cameraGameObject.name}' does not have a Camera component."
                            : "No camera found. Provide 'cameraPath'/'cameraInstanceId', or tag a camera 'MainCamera'.",
                        "not_found_error"
                    );
                }
            }
            else
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Invalid 'source' value: '{source}'. Expected 'scene_view' or 'camera'",
                    "validation_error"
                );
            }

            RenderTexture renderTexture = null;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTargetTexture = camera.targetTexture;
            Texture2D screenshot = null;

            try
            {
                renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
                screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenshot.Apply();

                byte[] imageBytes = format == "jpg" ? screenshot.EncodeToJPG(90) : screenshot.EncodeToPNG();
                string base64 = Convert.ToBase64String(imageBytes);
                string mimeType = format == "jpg" ? "image/jpeg" : "image/png";

                McpLogger.LogInfo($"Captured {width}x{height} screenshot from '{camera.name}' ({source})");

                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "image",
                    ["message"] = $"Successfully captured a {width}x{height} screenshot from '{camera.name}'",
                    ["data"] = base64,
                    ["mimeType"] = mimeType,
                    ["width"] = width,
                    ["height"] = height
                };
            }
            finally
            {
                camera.targetTexture = previousTargetTexture;
                RenderTexture.active = previousActive;

                if (renderTexture != null)
                {
                    RenderTexture.ReleaseTemporary(renderTexture);
                }

                if (screenshot != null)
                {
                    UnityEngine.Object.DestroyImmediate(screenshot);
                }
            }
        }
    }
}
