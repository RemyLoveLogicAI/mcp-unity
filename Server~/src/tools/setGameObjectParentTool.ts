import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the tool
const toolName = 'set_gameobject_parent';
const toolDescription = "Sets a GameObject's parent in the hierarchy, or moves it to the scene root when 'parentInstanceId'/'parentObjectPath' are omitted";
const paramsSchema = z.object({
  instanceId: z.number().optional().describe('The instance ID of the GameObject to reparent'),
  objectPath: z.string().optional().describe('The path of the GameObject in the hierarchy (alternative to instanceId)'),
  parentInstanceId: z.number().optional().describe('The instance ID of the new parent GameObject (omit along with parentObjectPath to move to the scene root)'),
  parentObjectPath: z.string().optional().describe('The path of the new parent GameObject in the hierarchy (alternative to parentInstanceId)'),
  worldPositionStays: z.boolean().optional().describe('Whether the GameObject should keep its world position when reparented (default true)')
});

/**
 * Creates and registers the Set GameObject Parent tool with the MCP server
 * This tool allows changing a GameObject's parent in the hierarchy in the Unity Editor
 *
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerSetGameObjectParentTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
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
 * Handles reparenting a GameObject in Unity
 *
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param params The parameters for the tool
 * @returns A promise that resolves to the tool execution result
 * @throws McpUnityError if the request to Unity fails
 */
async function toolHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  // Validate parameters - require either instanceId or objectPath
  if ((params.instanceId === undefined || params.instanceId === null) &&
      (!params.objectPath || params.objectPath.trim() === '')) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Either 'instanceId' or 'objectPath' must be provided to identify the GameObject to reparent"
    );
  }

  // Send request to Unity
  const response = await mcpUnity.sendRequest({
    method: toolName,
    params: {
      instanceId: params.instanceId,
      objectPath: params.objectPath,
      parentInstanceId: params.parentInstanceId,
      parentObjectPath: params.parentObjectPath,
      worldPositionStays: params.worldPositionStays
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || `Failed to set GameObject parent`
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || `Successfully reparented GameObject`
    }]
  };
}
