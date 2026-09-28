import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the tool
const toolName = 'clear_console';
const toolDescription = "Clears the Unity Editor console and the MCP server's captured log history";
const paramsSchema = z.object({});

/**
 * Creates and registers the Clear Console tool with the MCP server
 * This tool allows clearing the Unity Editor console
 *
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerClearConsoleTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);

  server.tool(
    toolName,
    toolDescription,
    paramsSchema.shape,
    async () => {
      try {
        logger.info(`Executing tool: ${toolName}`);
        const result = await toolHandler(mcpUnity);
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
 * Handles clearing the Unity console
 *
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @returns A promise that resolves to the tool execution result
 * @throws McpUnityError if the request to Unity fails
 */
async function toolHandler(mcpUnity: McpUnity): Promise<CallToolResult> {
  const response = await mcpUnity.sendRequest({
    method: toolName,
    params: {}
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to clear the console'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || 'Successfully cleared the console'
    }]
  };
}
