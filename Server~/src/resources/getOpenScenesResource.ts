import { McpUnity } from '../unity/mcpUnity.js';
import { Logger } from '../utils/logger.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { ReadResourceResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the resource
const resourceName = 'get_open_scenes';
const resourceUri = 'unity://scenes';
const resourceMimeType = 'application/json';

/**
 * Creates and registers the Open Scenes resource with the MCP server
 * This resource provides access to the list of currently loaded Unity scenes
 *
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerGetOpenScenesResource(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering resource: ${resourceName}`);

  server.resource(
    resourceName,
    resourceUri,
    {
      description: 'Retrieves the list of currently loaded scenes, including which one is active',
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
 * Handles requests for the currently loaded Unity scenes
 *
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @returns A promise that resolves to the open scenes data
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
      response.message || 'Failed to fetch open scenes'
    );
  }

  return {
    contents: [
      {
        uri: resourceUri,
        text: JSON.stringify(response.scenes ?? [], null, 2),
        mimeType: resourceMimeType
      }
    ]
  };
}
