import { McpUnity } from '../unity/mcpUnity.js';
import { Logger } from '../utils/logger.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { ReadResourceResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the resource
const resourceName = 'get_component_types';
const resourceUri = 'unity://component-types';
const resourceMimeType = 'application/json';

/**
 * Creates and registers the Component Types resource with the MCP server
 * This resource provides access to the non-abstract Component-derived types available in the project
 *
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerGetComponentTypesResource(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering resource: ${resourceName}`);

  // Register this resource with the MCP server
  server.resource(
    resourceName,
    resourceUri,
    {
      description: 'Retrieve the names of all non-abstract Component-derived types available in the project, for use with update_component/destroy_component',
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
 * Handles requests for the list of available component types from Unity
 *
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @returns A promise that resolves to the component types data
 * @throws McpUnityError if the request to Unity fails
 */
async function resourceHandler(mcpUnity: McpUnity): Promise<ReadResourceResult> {
      // Since we're using a non-templated ResourceDefinition, we need to handle all component types without parameters
      const response = await mcpUnity.sendRequest({
        method: resourceName,
        params: {}
      });

      if (!response.success) {
        throw new McpUnityError(
          ErrorType.RESOURCE_FETCH,
          response.message || 'Failed to fetch component types from Unity'
        );
      }

      const componentTypes = response.componentTypes || [];

      return {
        contents: [
          {
            uri: resourceUri,
            mimeType: resourceMimeType,
            text: JSON.stringify({ componentTypes }, null, 2)
          }
        ]
      };
}
