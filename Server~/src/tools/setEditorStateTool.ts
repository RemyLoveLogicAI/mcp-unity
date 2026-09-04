import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the tool
const toolName = 'set_editor_state';
const toolDescription = "Controls the Unity Editor's play mode state (play, pause, resume, or stop). " +
  "Play/stop transitions are asynchronous in Unity, so the isPlaying/isPaused values in the response " +
  "reflect the transition as requested and may not be fully applied yet; read the unity://editor-state " +
  "resource to confirm the observed state.";
const paramsSchema = z.object({
  state: z.enum(['play', 'pause', 'resume', 'stop']).describe("The desired editor state: 'play', 'pause', 'resume', or 'stop'")
});

/**
 * Creates and registers the Set Editor State tool with the MCP server
 * This tool allows controlling the Unity Editor's play mode (start/pause/stop)
 *
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerSetEditorStateTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);

  // Register this tool with the MCP server
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
 * Handles setting the Unity Editor's play mode state
 *
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param params The parameters for the tool
 * @returns A promise that resolves to the tool execution result
 * @throws McpUnityError if the request to Unity fails
 */
async function toolHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  const response = await mcpUnity.sendRequest({
    method: toolName,
    params
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || `Failed to set editor state`
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || `Successfully set editor state`
    }]
  };
}
