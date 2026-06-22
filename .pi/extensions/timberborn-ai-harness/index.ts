import type { ExtensionAPI } from "@earendil-works/pi-coding-agent";
import { Type } from "typebox";

const DEFAULT_BASE_URL = process.env.TIMBERBORN_AI_URL ?? "http://localhost:8080";

type QueryValue = boolean | number | string | undefined;

async function callHarness(path: string, params: Record<string, QueryValue> = {}, method = "POST"): Promise<unknown> {
  const url = new URL(path, DEFAULT_BASE_URL);
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined) {
      url.searchParams.set(key, String(value));
    }
  }

  const response = await fetch(url, { method });
  const text = await response.text();
  let payload: unknown;
  try {
    payload = JSON.parse(text);
  } catch {
    payload = { ok: false, error: text };
  }

  if (!response.ok) {
    throw new Error(JSON.stringify(payload, null, 2));
  }

  return payload;
}

function jsonText(payload: unknown): string {
  return JSON.stringify(payload, null, 2);
}

export default function timberbornAiHarness(pi: ExtensionAPI): void {
  pi.registerTool({
    name: "timberborn_status",
    label: "Timberborn Status",
    description: "Read live Timberborn AI Harness status from localhost.",
    parameters: Type.Object({}),
    async execute() {
      const payload = await callHarness("/api/ai-harness/status", {}, "GET");
      return {
        content: [{ type: "text", text: jsonText(payload) }],
        details: payload,
      };
    },
  });

  pi.registerTool({
    name: "timberborn_list_buildings",
    label: "List Timberborn Buildings",
    description: "List placeable Timberborn building templates known to the AI Harness.",
    parameters: Type.Object({
      query: Type.Optional(Type.String({ description: "Optional template search text, such as water or storage." })),
    }),
    async execute(_toolCallId, params) {
      const payload = await callHarness("/api/ai-harness/buildings", { query: params.query }, "GET");
      return {
        content: [{ type: "text", text: jsonText(payload) }],
        details: payload,
      };
    },
  });

  pi.registerTool({
    name: "timberborn_place_building",
    label: "Place Timberborn Building",
    description:
      "Place a Timberborn building or construction site through the AI Harness. Use template water_tank for a simple water tank. If x/y/z are omitted, the harness searches near the current camera.",
    promptSnippet: "timberborn_place_building: place a building in the live Timberborn game through AI Harness.",
    promptGuidelines: [
      "Use timberborn_place_building when asked to place a Timberborn building.",
      "For a simple water tank, pass template: water_tank.",
      "Omit x, y, and z unless the user provides exact coordinates; the harness can search near the current camera.",
    ],
    parameters: Type.Object({
      template: Type.Optional(Type.String({ description: "Building template or alias. Use water_tank for a simple water tank." })),
      x: Type.Optional(Type.Integer({ description: "Optional block x coordinate." })),
      y: Type.Optional(Type.Integer({ description: "Optional block y coordinate." })),
      z: Type.Optional(Type.Integer({ description: "Optional block z coordinate." })),
      orientation: Type.Optional(
        Type.Union([
          Type.Literal("Cw0"),
          Type.Literal("Cw90"),
          Type.Literal("Cw180"),
          Type.Literal("Cw270"),
        ]),
      ),
      flipped: Type.Optional(Type.Boolean({ description: "Whether to flip the building footprint." })),
      searchRadius: Type.Optional(Type.Integer({ minimum: 1, maximum: 64, description: "Auto-placement search radius." })),
    }),
    async execute(_toolCallId, params) {
      const payload = await callHarness("/api/ai-harness/place-building", {
        template: params.template ?? "water_tank",
        x: params.x,
        y: params.y,
        z: params.z,
        orientation: params.orientation ?? "Cw0",
        flipped: params.flipped ?? false,
        searchRadius: params.searchRadius ?? 16,
      });
      return {
        content: [{ type: "text", text: jsonText(payload) }],
        details: payload,
      };
    },
  });
}
