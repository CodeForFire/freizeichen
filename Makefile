SLN := Freizeichen.slnx
APP := src/Freizeichen.App/Freizeichen.App.csproj
SHIM := tools/obj/pty-modem-lines.so
SCENARIO ?= ok
PHONE_PORT ?= /dev/rfcomm9

.PHONY: help restore build test ci format format-check run fake-phone run-fake clean

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

run-fake: $(SHIM) ## Run the app so it can open the fake phone's pseudo-terminal (Linux)
	LD_PRELOAD=$(CURDIR)/$(SHIM) dotnet run --project $(APP) -- $(ARGS)

$(SHIM): tools/pty-modem-lines.c
	mkdir -p $(@D)
	cc -Wall -Wextra -shared -fPIC -o $@ $< -ldl

clean: ## Remove build output
	find src tests tools -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
