import { McpUnity } from '../unity/mcpUnity.js';
import { Logger } from '../utils/logger.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { ReadResourceResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the resource
const resourceName = 'get_editor_state';
const resourceUri = 'unity://editor-state';
const resourceMimeType = 'application/json';

/**
 * Creates and registers the Editor State resource with the MCP server
 * This resource provides access to the current Unity Editor application state
 *
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerGetEditorStateResource(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering resource: ${resourceName}`);

  // Register this resource with the MCP server
  server.resource(
    resourceName,
    resourceUri,
    {
      description: 'Retrieves the current state of the Unity Editor (playmode, paused, compiling)',
      mimeType: resourceMimeType
    },
    async () => {
      try {
        return await resourceHandler(mcpUnity);
      } catch (error) {
        logger.error(`Error handling resource ${resourceName}: ${error}`);
        throw error;
      }
    }
  );
}

/**
 * Handles requests for the current Unity Editor application state
 *
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @returns A promise that resolves to the editor state data
 * @throws McpUnityError if the request to Unity fails
 */
async function resourceHandler(mcpUnity: McpUnity): Promise<ReadResourceResult> {
  const response = await mcpUnity.sendRequest({
    method: resourceName,
    params: {}
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.RESOURCE_FETCH,
      response.message || 'Failed to fetch Unity Editor state'
    );
  }

  const editorStateData = {
    isPlaying: response.isPlaying,
    isPaused: response.isPaused,
    isCompiling: response.isCompiling,
    applicationPath: response.applicationPath,
    unityVersion: response.unityVersion
  };

  return {
    contents: [
      {
        uri: resourceUri,
        text: JSON.stringify(editorStateData, null, 2),
        mimeType: resourceMimeType
      }
    ]
  };
}
