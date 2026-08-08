PROJECT := KrokoshaTeleport.csproj
CONFIGURATION ?= Release
GAME_DIR ?= F:/exe/a/steamapps/common/Casualties Unknown Demo

DOTNET ?= dotnet

BUILD_ARGS := \
	"$(PROJECT)" \
	-c $(CONFIGURATION) \
	-p:GameDir="$(GAME_DIR)"

.PHONY: all restore build rebuild clean install info

all: build

restore:
	$(DOTNET) restore $(BUILD_ARGS)

build:
	$(DOTNET) build $(BUILD_ARGS)

rebuild:
	$(DOTNET) build $(BUILD_ARGS) --no-restore -t:Rebuild

clean:
	$(DOTNET) clean $(BUILD_ARGS)

install: build
	@echo Plugin copied to the BepInEx plugins directory by the project file.

info:
	@echo Project: $(PROJECT)
	@echo Configuration: $(CONFIGURATION)
	@echo Game directory: $(GAME_DIR)