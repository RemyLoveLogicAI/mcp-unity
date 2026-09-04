import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the tool
const toolName = 'move_asset';
const toolDescription = 'Moves or renames an asset from one path to another in the Unity project';
const paramsSchema = z.object({
  sourcePath: z.string().describe('Assets path of the asset to move (e.g. "Assets/Materials/Red.mat")'),
  destinationPath: z.string().describe('New Assets path for the asset (e.g. "Assets/Materials/Crimson.mat")')
});

/**
 * Creates and registers the Move Asset tool with the MCP server
 * This tool allows moving or renaming an asset in the Unity project
 *
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerMoveAssetTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
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
 * Handles moving an asset in Unity
 *
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param params The parameters for the tool
 * @returns A promise that resolves to the tool execution result
 * @throws McpUnityError if the request to Unity fails
 */
async function toolHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  if (!params.sourcePath || !params.destinationPath) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Both 'sourcePath' and 'destinationPath' must be provided"
    );
  }

  const response = await mcpUnity.sendRequest({
    method: toolName,
    params
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || `Failed to move asset`
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || `Successfully moved asset`
    }]
  };
}
