import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the tool
const toolName = 'create_scene';
const toolDescription = 'Creates a new empty scene with default GameObjects and saves it to the given Assets path';
const paramsSchema = z.object({
  path: z.string().describe('Assets path to save the new scene to (e.g. "Assets/Scenes/NewScene.unity")'),
  loadAdditively: z.boolean().optional().describe('Whether to add the new scene to the currently loaded scenes instead of replacing them')
});

/**
 * Creates and registers the Create Scene tool with the MCP server
 * This tool allows creating a new empty scene in the Unity project
 *
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerCreateSceneTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
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
 * Handles creating a new scene in Unity
 *
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param params The parameters for the tool
 * @returns A promise that resolves to the tool execution result
 * @throws McpUnityError if the request to Unity fails
 */
async function toolHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  if (!params.path || params.path.trim() === '') {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'path' not provided"
    );
  }

  const response = await mcpUnity.sendRequest({
    method: toolName,
    params
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || `Failed to create scene`
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || `Successfully created scene`
    }]
  };
}
