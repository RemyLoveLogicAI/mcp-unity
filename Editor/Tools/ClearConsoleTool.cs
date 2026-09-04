using System;
using System.Reflection;
using McpUnity.Services;
using McpUnity.Unity;
using McpUnity.Utils;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for clearing the Unity Editor console
    /// </summary>
    public class ClearConsoleTool : McpToolBase
    {
        private readonly IConsoleLogsService _consoleLogsService;

        public ClearConsoleTool(IConsoleLogsService consoleLogsService)
        {
            Name = "clear_console";
            Description = "Clears the Unity Editor console and the MCP server's captured log history";

            _consoleLogsService = consoleLogsService;
        }

        /// <summary>
        /// Execute the ClearConsole tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject (none expected)</param>
        public override JObject Execute(JObject parameters)
        {
            _consoleLogsService.ClearLogs();

            bool unityConsoleCleared = false;
            try
            {
                // UnityEditor.LogEntries is an internal API, so it must be reached via reflection
                Type logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor");
                MethodInfo clearMethod = logEntriesType?.GetMethod("Clear",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                clearMethod?.Invoke(null, null);
                unityConsoleCleared = clearMethod != null;
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"Cleared MCP log history, but failed to clear the Unity console window: {ex.Message}");
            }

            return new JObject
            {
                ["success"] = unityConsoleCleared,
                ["type"] = "text",
                ["message"] = unityConsoleCleared
                    ? "Successfully cleared the console"
                    : "Cleared MCP log history, but failed to clear the Unity Editor console window"
            };
        }
    }
}
