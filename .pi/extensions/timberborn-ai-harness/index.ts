import type { ExtensionAPI } from "@earendil-works/pi-coding-agent";
import { Type } from "typebox";

const DEFAULT_BASE_URL = process.env.TIMBERBORN_AI_URL ?? "http://localhost:8080";

type QueryValue = boolean | number | string | undefined;

type MenuOption = {
  label: string;
  kind: string;
  payload?: string;
};

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

function normalize(value: string | undefined): string {
  return (value ?? "").toLowerCase().replace(/[^a-z0-9]/g, "");
}

function validateFourButtonMenu(options: MenuOption[]): void {
  if (options.length !== 4) {
    throw new Error("Timberborn Pi menus must have exactly four options.");
  }

  const normalized = options.map((option) => ({
    kind: normalize(option.kind),
    label: normalize(option.label),
  }));
  const confirmation = normalized.some((option) => option.kind === "confirm");
  const hasTool = normalized.some((option) => option.kind === "tool");
  const hasNavigation = normalized.some((option) => ["menu", "nav", "navigate"].includes(option.kind));
  const hasBackOrNo = normalized.some(
    (option) =>
      ["back", "cancel", "no"].includes(option.kind) ||
      option.label.includes("back") ||
      option.label.includes("cancel") ||
      option.label === "no",
  );
  const hasYes = normalized.some((option) => option.kind === "yes" || option.label === "yes" || option.label.startsWith("yes"));
  const hasNo = normalized.some((option) => option.kind === "no" || option.label === "no" || option.label.startsWith("no"));

  if (confirmation && (!hasYes || !hasNo)) {
    throw new Error("Confirmation menus must include yes and no choices.");
  }

  if (!confirmation && (!hasTool || !hasNavigation || !hasBackOrNo)) {
    throw new Error("Non-confirmation menus must include at least one tool option, one menu/navigation option, and one back/cancel/no option.");
  }
}

async function sleep(ms: number): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, ms));
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
      "For basic building helpers, templates path, stairs, and platform are supported aliases.",
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

  pi.registerTool({
    name: "timberborn_interaction_state",
    label: "Timberborn Pi Interaction State",
    description: "Read the current four-button Pi interaction state from the in-game HUD loop.",
    parameters: Type.Object({}),
    async execute() {
      const payload = await callHarness("/api/ai-harness/interaction", {}, "GET");
      return {
        content: [{ type: "text", text: jsonText(payload) }],
        details: payload,
      };
    },
  });

  pi.registerTool({
    name: "timberborn_show_menu",
    label: "Show Timberborn Four-Button Menu",
    description: "Show a contextual Pi menu in Timberborn. Exactly four options are required.",
    promptGuidelines: [
      "Every menu must have exactly four options.",
      "Non-confirmation menus must mix a deterministic tool option, a menu/navigation option, and a back/cancel/no option.",
      "Before a mutating tool use, show a confirmation menu that includes yes and no choices.",
    ],
    parameters: Type.Object({
      interactionId: Type.Optional(Type.String()),
      menuId: Type.Optional(Type.String()),
      question: Type.String(),
      skillId: Type.Optional(Type.String()),
      menuPath: Type.Optional(Type.String()),
      contextHash: Type.Optional(Type.String()),
      options: Type.Array(
        Type.Object({
          label: Type.String(),
          kind: Type.String({ description: "Use tool, menu, back, cancel, yes, no, or confirm." }),
          payload: Type.Optional(Type.String()),
        }),
        { minItems: 4, maxItems: 4 },
      ),
    }),
    async execute(_toolCallId, params) {
      const options = params.options as MenuOption[];
      validateFourButtonMenu(options);
      const payload = await callHarness("/api/ai-harness/interaction/show", {
        interactionId: params.interactionId,
        menuId: params.menuId ?? "pi-menu",
        question: params.question,
        skillId: params.skillId ?? "general",
        menuPath: params.menuPath ?? "root",
        contextHash: params.contextHash ?? "pi",
        label1: options[0].label,
        kind1: options[0].kind,
        payload1: options[0].payload ?? "",
        label2: options[1].label,
        kind2: options[1].kind,
        payload2: options[1].payload ?? "",
        label3: options[2].label,
        kind3: options[2].kind,
        payload3: options[2].payload ?? "",
        label4: options[3].label,
        kind4: options[3].kind,
        payload4: options[3].payload ?? "",
      });
      return {
        content: [{ type: "text", text: jsonText(payload) }],
        details: payload,
      };
    },
  });

  pi.registerTool({
    name: "timberborn_wait_for_choice",
    label: "Wait For Timberborn Four-Button Choice",
    description: "Poll the HUD loop until the player answers with one of the four in-game buttons.",
    parameters: Type.Object({
      timeoutMs: Type.Optional(Type.Integer({ minimum: 1000, maximum: 120000 })),
      pollMs: Type.Optional(Type.Integer({ minimum: 250, maximum: 5000 })),
    }),
    async execute(_toolCallId, params) {
      const timeoutMs = params.timeoutMs ?? 60000;
      const pollMs = params.pollMs ?? 1000;
      const deadline = Date.now() + timeoutMs;
      let payload: unknown = await callHarness("/api/ai-harness/interaction", {}, "GET");
      while (Date.now() < deadline) {
        const state = payload as { data?: { status?: string; lastButton?: number } };
        const data = state.data;
        if (data && (data.status === "answerSubmitted" || data.status === "toolRequested") && data.lastButton) {
          return {
            content: [{ type: "text", text: jsonText(payload) }],
            details: payload,
          };
        }

        await sleep(pollMs);
        payload = await callHarness("/api/ai-harness/interaction", {}, "GET");
      }

      throw new Error("Timed out waiting for a Timberborn four-button answer.");
    },
  });

  pi.registerTool({
    name: "timberborn_record_tool_result",
    label: "Record Timberborn Tool Result",
    description: "Record the result of a Pi tool call back into the four-button in-game loop.",
    parameters: Type.Object({
      tool: Type.String(),
      ok: Type.Boolean(),
      summary: Type.Optional(Type.String()),
    }),
    async execute(_toolCallId, params) {
      const payload = await callHarness("/api/ai-harness/interaction/tool-result", {
        tool: params.tool,
        ok: params.ok,
        summary: params.summary ?? "",
      });
      return {
        content: [{ type: "text", text: jsonText(payload) }],
        details: payload,
      };
    },
  });

  pi.registerTool({
    name: "timberborn_game_context",
    label: "Timberborn Game Context",
    description: "Read deterministic day, speed, camera, district, and population context from the live game.",
    parameters: Type.Object({}),
    async execute() {
      const payload = await callHarness("/api/ai-harness/game-context", {}, "GET");
      return {
        content: [{ type: "text", text: jsonText(payload) }],
        details: payload,
      };
    },
  });

  pi.registerTool({
    name: "timberborn_resource_summary",
    label: "Timberborn Resource Summary",
    description: "Read deterministic stock, capacity, fill-rate, carried, and buffered counts for a Timberborn good.",
    parameters: Type.Object({
      good: Type.Optional(Type.String({ description: "Good id or search text, default water." })),
    }),
    async execute(_toolCallId, params) {
      const payload = await callHarness("/api/ai-harness/resource-summary", { good: params.good ?? "water" }, "GET");
      return {
        content: [{ type: "text", text: jsonText(payload) }],
        details: payload,
      };
    },
  });

  pi.registerTool({
    name: "timberborn_water_readiness",
    label: "Timberborn Water Readiness",
    description: "Compute deterministic water-per-beaver, water-capacity-per-beaver, and days-of-water metrics.",
    parameters: Type.Object({}),
    async execute() {
      const payload = await callHarness("/api/ai-harness/water-readiness", {}, "GET");
      return {
        content: [{ type: "text", text: jsonText(payload) }],
        details: payload,
      };
    },
  });
}
