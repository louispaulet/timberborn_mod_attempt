SHELL := /bin/bash

GAME_APP := /Users/louispaulet/Library/Application Support/Steam/steamapps/common/Timberborn/Timberborn.app
MANAGED_DIR := $(GAME_APP)/Contents/Resources/Data/Managed
MODS_DIR := /Users/louispaulet/Documents/Timberborn/Mods
PLAYER_LOG := /Users/louispaulet/Library/Logs/Mechanistry/Timberborn/Player.log
LOOP_PID := $(CURDIR)/.tools/timberborn-pi-companion.pid
LOOP_LOG := $(CURDIR)/logs/timberborn-pi-companion.log
FAKE_HARNESS_PID := $(CURDIR)/.tools/fake-timberborn-harness.pid
FAKE_HARNESS_LOG := $(CURDIR)/logs/fake-timberborn-harness.log
FAKE_HARNESS_URL := http://127.0.0.1:18080

MOD_NAME := AiHarness
PROJECT := src/AiHarness.Mod/AiHarness.Mod.csproj
TEST_PROJECT := tests/AiHarness.Core.Tests/AiHarness.Core.Tests.csproj
DLL := src/AiHarness.Mod/bin/Release/netstandard2.1/AiHarness.Mod.dll
CORE_DLL := src/AiHarness.Mod/bin/Release/netstandard2.1/AiHarness.Core.dll
PACKAGE_DIR := dist/AiHarness

LOCAL_DOTNET := $(CURDIR)/.tools/dotnet/dotnet
DOTNET := $(shell command -v dotnet 2>/dev/null || printf '%s' '$(LOCAL_DOTNET)')
PYTHON ?= python3

.PHONY: verify-env bootstrap test-fast watch-fast smoke-fake verify-fast smoke-live build package install launch up down logs clean

verify-env:
	@test -d "$(GAME_APP)" || (echo "Timberborn app not found: $(GAME_APP)" && exit 1)
	@test -d "$(MANAGED_DIR)" || (echo "Managed DLL directory not found: $(MANAGED_DIR)" && exit 1)
	@test -f "$(MANAGED_DIR)/Timberborn.ModManagerScene.dll" || (echo "Missing Timberborn.ModManagerScene.dll" && exit 1)
	@test -f "$(MANAGED_DIR)/Timberborn.CoreUI.dll" || (echo "Missing Timberborn.CoreUI.dll" && exit 1)
	@test -f "$(MANAGED_DIR)/Timberborn.HttpApiSystem.dll" || (echo "Missing Timberborn.HttpApiSystem.dll" && exit 1)
	@test -f "$(MANAGED_DIR)/Timberborn.TimeSystem.dll" || (echo "Missing Timberborn.TimeSystem.dll" && exit 1)
	@test -f "$(MANAGED_DIR)/Timberborn.CameraSystem.dll" || (echo "Missing Timberborn.CameraSystem.dll" && exit 1)
	@mkdir -p "$(MODS_DIR)"
	@echo "Timberborn app: $(GAME_APP)"
	@echo "Managed DLLs: $(MANAGED_DIR)"
	@echo "Mods directory: $(MODS_DIR)"
	@plutil -p "$(GAME_APP)/Contents/Info.plist" | grep CFBundleShortVersionString || true

bootstrap:
	@if command -v dotnet >/dev/null 2>&1; then \
		echo "Using system dotnet: $$(command -v dotnet)"; \
	else \
		mkdir -p "$(CURDIR)/.tools"; \
		if [ ! -x "$(LOCAL_DOTNET)" ]; then \
			echo "Installing local .NET SDK into .tools/dotnet"; \
			curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$(CURDIR)/.tools/dotnet-install.sh"; \
			bash "$(CURDIR)/.tools/dotnet-install.sh" --channel 8.0 --install-dir "$(CURDIR)/.tools/dotnet"; \
		fi; \
		"$(LOCAL_DOTNET)" --info; \
	fi

build: bootstrap verify-env
	"$(DOTNET)" build "$(PROJECT)" -c Release

test-fast: bootstrap
	"$(DOTNET)" test "$(TEST_PROJECT)" -c Release

watch-fast: bootstrap
	"$(DOTNET)" watch --project "$(TEST_PROJECT)" test -c Release

smoke-fake:
	@mkdir -p "$(CURDIR)/.tools" "$(CURDIR)/logs"
	@set -euo pipefail; \
	rm -f "$(FAKE_HARNESS_PID)"; \
	"$(CURDIR)/scripts/fake-timberborn-harness" --port 18080 > "$(FAKE_HARNESS_LOG)" 2>&1 & \
	pid="$$!"; \
	echo "$$pid" > "$(FAKE_HARNESS_PID)"; \
	cleanup() { kill "$$pid" >/dev/null 2>&1 || true; wait "$$pid" >/dev/null 2>&1 || true; rm -f "$(FAKE_HARNESS_PID)"; }; \
	trap cleanup EXIT; \
	for attempt in {1..50}; do \
		if "$(CURDIR)/scripts/timberborn-ai" --base-url "$(FAKE_HARNESS_URL)" --compact status >/dev/null 2>&1; then \
			break; \
		fi; \
		sleep 0.1; \
	done; \
	"$(CURDIR)/scripts/timberborn-ai" --base-url "$(FAKE_HARNESS_URL)" --compact status >/dev/null; \
	"$(CURDIR)/scripts/timberborn-ai" --base-url "$(FAKE_HARNESS_URL)" --compact commands >/dev/null; \
	"$(CURDIR)/scripts/timberborn-ai" --base-url "$(FAKE_HARNESS_URL)" --compact interaction-clear >/dev/null; \
	"$(CURDIR)/scripts/timberborn-ai" --base-url "$(FAKE_HARNESS_URL)" --compact interaction-request --topic "fake smoke" >/dev/null; \
	"$(CURDIR)/scripts/timberborn-pi-companion" --base-url "$(FAKE_HARNESS_URL)" --allow-no-game --once >/dev/null; \
	"$(CURDIR)/scripts/timberborn-ai" --base-url "$(FAKE_HARNESS_URL)" --compact interaction-answer 1 >/dev/null; \
	"$(CURDIR)/scripts/timberborn-pi-companion" --base-url "$(FAKE_HARNESS_URL)" --allow-no-game --once >/dev/null; \
	"$(CURDIR)/scripts/timberborn-ai" --base-url "$(FAKE_HARNESS_URL)" --compact water-readiness >/dev/null; \
	"$(CURDIR)/scripts/timberborn-ai" --base-url "$(FAKE_HARNESS_URL)" --compact buildings tank >/dev/null; \
	if "$(CURDIR)/scripts/timberborn-ai" --base-url "$(FAKE_HARNESS_URL)" --compact place-building definitely-not-a-template >/dev/null 2>&1; then \
		echo "Expected fake invalid placement to fail."; \
		exit 1; \
	fi; \
	"$(CURDIR)/scripts/timberborn-ai" --base-url "$(FAKE_HARNESS_URL)" --compact status >/dev/null; \
	echo "Fake harness smoke passed: $(FAKE_HARNESS_URL)"

verify-fast: test-fast smoke-fake build

smoke-live:
	@if ! pgrep -x Timberborn >/dev/null 2>&1; then \
		echo "Timberborn is not running; launch it once, enable the mod, then rerun make smoke-live."; \
		exit 2; \
	fi
	@set -euo pipefail; \
	base="$${TIMBERBORN_AI_URL:-http://localhost:8080}"; \
	status_json="$$("$(CURDIR)/scripts/timberborn-ai" --base-url "$$base" --compact status)"; \
	echo "$$status_json"; \
	if printf '%s' "$$status_json" | "$(PYTHON)" -c 'import json,sys; data=json.load(sys.stdin).get("data") or {}; sys.exit(0 if data.get("context") == "MainMenu" else 1)'; then \
		settlement="AiHarnessSmoke-$$(date +%Y%m%d%H%M%S)"; \
		echo "Main menu detected; creating disposable settlement $$settlement"; \
		"$(CURDIR)/scripts/timberborn-ai" --base-url "$$base" --compact new-game --settlement "$$settlement" --map Diorama --faction Folktails >/dev/null; \
	fi; \
	ready=0; \
	for attempt in {1..60}; do \
		if "$(CURDIR)/scripts/timberborn-ai" --base-url "$$base" --compact water-readiness >/dev/null 2>&1; then \
			ready=1; \
			break; \
		fi; \
		sleep 1; \
	done; \
	if [ "$$ready" != "1" ]; then \
		echo "Timed out waiting for game-context harness endpoints."; \
		exit 1; \
	fi; \
	"$(CURDIR)/scripts/timberborn-ai" --base-url "$$base" --compact commands >/dev/null; \
	"$(CURDIR)/scripts/timberborn-ai" --base-url "$$base" --compact interaction-clear >/dev/null; \
	"$(CURDIR)/scripts/timberborn-ai" --base-url "$$base" --compact interaction-request --topic "live smoke" >/dev/null; \
	"$(CURDIR)/scripts/timberborn-ai" --base-url "$$base" --compact interaction-show --question "Smoke check?" --label "Water readiness" --kind tool --payload timberborn_water_readiness --label "Build tips" --kind menu --payload building.pathing --label "Game context" --kind tool --payload timberborn_game_context --label "No" --kind no >/dev/null; \
	"$(CURDIR)/scripts/timberborn-ai" --base-url "$$base" --compact interaction-answer 1 >/dev/null; \
	"$(CURDIR)/scripts/timberborn-ai" --base-url "$$base" --compact interaction-tool-result timberborn_water_readiness --ok --summary "Live smoke checked water readiness." >/dev/null; \
	if "$(CURDIR)/scripts/timberborn-ai" --base-url "$$base" --compact place-building definitely-not-a-template >/dev/null 2>&1; then \
		echo "Expected live invalid placement to fail."; \
		exit 1; \
	fi; \
	"$(CURDIR)/scripts/timberborn-ai" --base-url "$$base" --compact status >/dev/null; \
	echo "Live harness smoke passed: $$base"

package: build
	rm -rf "$(PACKAGE_DIR)"
	mkdir -p "$(PACKAGE_DIR)"
	cp mod/manifest.json "$(PACKAGE_DIR)/manifest.json"
	cp "$(DLL)" "$(PACKAGE_DIR)/Code.dll"
	cp "$(CORE_DLL)" "$(PACKAGE_DIR)/AiHarness.Core.dll"
	@find "$(PACKAGE_DIR)" -maxdepth 2 -type f -print

install: package
	rm -rf "$(MODS_DIR)/HelloWorld" "$(MODS_DIR)/AiHarness"
	mkdir -p "$(MODS_DIR)"
	cp -R "$(PACKAGE_DIR)" "$(MODS_DIR)/AiHarness"
	@find "$(MODS_DIR)/AiHarness" -maxdepth 2 -type f -print

launch:
	open -a Steam
	sleep 2
	open steam://run/1062090

up:
	@mkdir -p "$(CURDIR)/.tools" "$(CURDIR)/logs"
	@if ! pgrep -x Timberborn >/dev/null 2>&1; then \
		echo "Timberborn is not running; refusing to start the Pi companion."; \
		echo "Launch Timberborn first, then run make up."; \
		exit 2; \
	fi
	@if [ -f "$(LOOP_PID)" ] && kill -0 "$$(cat "$(LOOP_PID)")" >/dev/null 2>&1; then \
		echo "Timberborn Pi companion already running with PID $$(cat "$(LOOP_PID)")"; \
	else \
		if [ -f "$(LOOP_PID)" ]; then rm -f "$(LOOP_PID)"; fi; \
		nohup "$(CURDIR)/scripts/timberborn-pi-companion" > "$(LOOP_LOG)" 2>&1 & \
		echo $$! > "$(LOOP_PID)"; \
		echo "Started Timberborn Pi companion with PID $$(cat "$(LOOP_PID)")"; \
		echo "Log: $(LOOP_LOG)"; \
	fi

down:
	@if [ -f "$(LOOP_PID)" ]; then \
		pid="$$(cat "$(LOOP_PID)")"; \
		if kill -0 "$$pid" >/dev/null 2>&1; then \
			kill "$$pid"; \
			echo "Stopped Timberborn Pi companion with PID $$pid"; \
		else \
			echo "Timberborn Pi companion PID $$pid is not running"; \
		fi; \
		rm -f "$(LOOP_PID)"; \
	else \
		echo "Timberborn Pi companion is not running (no PID file)"; \
	fi
	@pkill -TERM -f "$(CURDIR)/[s]cripts/timberborn-pi-companion" >/dev/null 2>&1 || true

logs:
	@test -f "$(PLAYER_LOG)" || (echo "Player log not found: $(PLAYER_LOG)" && exit 1)
	tail -n 200 "$(PLAYER_LOG)"

clean:
	rm -rf dist
	@if [ -d src ]; then find src -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +; fi
