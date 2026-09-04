import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the tool
const toolName = 'create_asset_folder';
const toolDescription = 'Creates a new folder at the given parent path in the Unity project';
const paramsSchema = z.object({
  parentPath: z.string().describe('Assets path of the existing parent folder (e.g. "Assets")'),
  folderName: z.string().describe('Name of the new folder to create (e.g. "Materials")')
});

/**
 * Creates and registers the Create Asset Folder tool with the MCP server
 * This tool allows creating a new folder in the Unity project
 *
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerCreateAssetFolderTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
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
 * Handles creating an asset folder in Unity
 *
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param params The parameters for the tool
 * @returns A promise that resolves to the tool execution result
 * @throws McpUnityError if the request to Unity fails
 */
async function toolHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  if (!params.parentPath || !params.folderName) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Both 'parentPath' and 'folderName' must be provided"
    );
  }

  const response = await mcpUnity.sendRequest({
    method: toolName,
    params
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || `Failed to create asset folder`
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || `Successfully created asset folder`
    }]
  };
}
