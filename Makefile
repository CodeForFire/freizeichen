SLN := Freizeichen.slnx
APP := src/Freizeichen.App/Freizeichen.App.csproj
SCENARIO ?= ok
PHONE_PORT ?= /dev/rfcomm9

.PHONY: help restore build test ci format format-check run fake-phone screenshots icon clean

help: ## Show this help
	@grep -E '^[a-zA-Z_-]+:.*?## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  %-14s %s\n", $$1, $$2}'

restore: ## Restore NuGet packages in locked mode, as CI does
	dotnet restore $(SLN) --locked-mode

build: ## Build everything (Release)
	dotnet build $(SLN) -c Release

test: ## Run the tests (FILTER=FullyQualifiedName~Foo to narrow)
	dotnet test $(SLN) -c Release $(if $(FILTER),--filter "$(FILTER)")

ci: restore ## Reproduce the CI legs locally: build, test, format check
	dotnet build $(SLN) -c Release --no-restore
	dotnet test $(SLN) -c Release --no-build
	dotnet format $(SLN) --verify-no-changes --no-restore

format: ## Apply dotnet format
	dotnet format $(SLN)

format-check: ## Fail if dotnet format would change anything
	dotnet format $(SLN) --verify-no-changes

run: ## Run the app (ARGS=tel:0891234567 to open the dial window)
	dotnet run --project $(APP) -- $(ARGS)

fake-phone: ## Play a DL500A on /dev/rfcomm9 (SCENARIO=ok|error|busy|nocarrier|nodialtone|silent; Linux)
	python3 tools/fake-dl500a.py --link $(PHONE_PORT) --scenario $(SCENARIO)

screenshots: ## Regenerate docs/screenshots/*.png for the README (ReadmeScreenshotTests with RENDER_OUT)
	RENDER_OUT=$(CURDIR)/docs/screenshots dotnet test tests/Freizeichen.App.Tests -c Release \
		--filter "FullyQualifiedName~ReadmeScreenshotTests"

icon: ## Re-render the committed icon derivatives from src/Freizeichen.App/Assets/icon.svg
	packaging/icon/build-icon-assets.sh

clean: ## Remove build output
	find src tests -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
