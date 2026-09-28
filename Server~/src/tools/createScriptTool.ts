import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the tool
const toolName = 'create_script';
const toolDescription = 'Creates a new C# script file with the given contents at the given Assets path';
const paramsSchema = z.object({
  path: z.string().describe('The Assets path for the new script (e.g. "Assets/Scripts/Player.cs")'),
  content: z.string().describe('The full contents of the script file')
});

/**
 * Creates and registers the Create Script tool with the MCP server
 * This tool allows creating a new C# script file in the Unity project
 *
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerCreateScriptTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
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
 * Handles creating a script file in Unity
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
      "Required parameter 'path' must be provided"
    );
  }

  if (params.content === undefined || params.content === null) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'content' must be provided"
    );
  }

  const response = await mcpUnity.sendRequest({
    method: toolName,
    params: {
      path: params.path,
      content: params.content
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || `Failed to create script '${params.path}'`
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || `Successfully created script '${params.path}'`
    }]
  };
}
