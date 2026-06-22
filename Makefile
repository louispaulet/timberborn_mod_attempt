SHELL := /bin/bash

GAME_APP := /Users/louispaulet/Library/Application Support/Steam/steamapps/common/Timberborn/Timberborn.app
MANAGED_DIR := $(GAME_APP)/Contents/Resources/Data/Managed
MODS_DIR := /Users/louispaulet/Documents/Timberborn/Mods
PLAYER_LOG := /Users/louispaulet/Library/Logs/Mechanistry/Timberborn/Player.log

MOD_NAME := AiHarness
PROJECT := src/AiHarness.Mod/AiHarness.Mod.csproj
DLL := src/AiHarness.Mod/bin/Release/netstandard2.1/AiHarness.Mod.dll
PACKAGE_DIR := dist/AiHarness

LOCAL_DOTNET := $(CURDIR)/.tools/dotnet/dotnet
DOTNET := $(shell command -v dotnet 2>/dev/null || printf '%s' '$(LOCAL_DOTNET)')

.PHONY: verify-env bootstrap build package install launch logs clean

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

package: build
	rm -rf "$(PACKAGE_DIR)"
	mkdir -p "$(PACKAGE_DIR)"
	cp mod/manifest.json "$(PACKAGE_DIR)/manifest.json"
	cp "$(DLL)" "$(PACKAGE_DIR)/Code.dll"
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

logs:
	@test -f "$(PLAYER_LOG)" || (echo "Player log not found: $(PLAYER_LOG)" && exit 1)
	tail -n 200 "$(PLAYER_LOG)"

clean:
	rm -rf dist
	@if [ -d src ]; then find src -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +; fi
