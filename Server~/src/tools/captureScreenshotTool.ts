import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the tool
const toolName = 'capture_screenshot';
const toolDescription = 'Captures a screenshot from the active Scene view camera, or from a specific or ' +
  'main scene camera, and returns it as an image';
const paramsSchema = z.object({
  source: z.enum(['scene_view', 'camera']).optional().describe(
    "Where to capture from: 'scene_view' (the active Scene view camera, default) or 'camera' " +
    "(a specific scene camera, or Camera.main if none given)"
  ),
  cameraPath: z.string().optional().describe("Path or name of the camera GameObject to capture from (only used when source='camera')"),
  cameraInstanceId: z.number().int().optional().describe("Instance ID of the camera GameObject to capture from (only used when source='camera')"),
  width: z.number().int().min(1).max(4096).optional().describe('Screenshot width in pixels (default 1024)'),
  height: z.number().int().min(1).max(4096).optional().describe('Screenshot height in pixels (default 768)'),
  format: z.enum(['png', 'jpg']).optional().describe("Image format (default 'png')")
});

/**
 * Creates and registers the Capture Screenshot tool with the MCP server
 * This tool allows capturing a screenshot from a Unity camera as an image
 *
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerCaptureScreenshotTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
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
 * Handles capturing a screenshot from a Unity camera
 *
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param params The parameters for the tool
 * @returns A promise that resolves to the tool execution result, with the screenshot as image content
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
      response.message || 'Failed to capture screenshot'
    );
  }

  if (response.type === 'image' && response.data) {
    return {
      content: [{
        type: 'image',
        data: response.data,
        mimeType: response.mimeType || 'image/png'
      }]
    };
  }

  return {
    content: [{
      type: 'text',
      text: response.message || 'Successfully captured screenshot'
    }]
  };
}
