import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the tool
const toolName = 'delete_asset';
const toolDescription = 'Deletes one or more assets by path. Moves them to the OS trash (recoverable) rather ' +
  'than permanently deleting.';
const paramsSchema = z.object({
  paths: z.array(z.string()).min(1).describe('Assets paths of the assets to delete (e.g. ["Assets/Materials/Old.mat"])')
});

/**
 * Creates and registers the Delete Asset tool with the MCP server
 * This tool allows deleting one or more assets in the Unity project
 *
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerDeleteAssetTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);

  server.tool(
    toolName,
    toolDescription,
    paramsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${toolName}`, params);
        const result = await toolHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${toolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${toolName}`, error);
        throw error;
      }
    }
  );
}

/**
 * Handles deleting assets in Unity
 *
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param params The parameters for the tool
 * @returns A promise that resolves to the tool execution result
 * @throws McpUnityError if the request to Unity fails
 */
async function toolHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  if (!Array.isArray(params.paths) || params.paths.length === 0) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'paths' must be a non-empty array"
    );
  }

  const response = await mcpUnity.sendRequest({
    method: toolName,
    params
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || `Failed to delete asset(s)`
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || `Successfully deleted asset(s)`
    }]
  };
}
