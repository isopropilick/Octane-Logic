# Octane Logic

Octane Logic is a Shapez 2 logic-expansion mod. Its first release establishes a deterministic, versioned logic engine for stateful gates and timing devices, using Shapez Shifter's supported mod-save-data API.

## What is implemented

### Placeable numeric signal buildings

The first physical buildings are available from the Signals toolbar. Each accepts integer
signals on its west (**A**) and south (**B**) connectors, and writes the result on its east
connector:

- **Adder** — `A + B`
- **Subtractor** — `A - B`
- **Multiplier** — `A × B`
- **Divider** — integer `A / B`; division by zero emits no signal.
- **Modulo** — integer `A mod B`; division by zero emits no signal.

Results clamp to the supported 32-bit integer range. Unsupported signal types emit no signal.
These deliberately complement the vanilla boolean gates rather than duplicate AND, OR, NOT,
XOR or equality.

### Deterministic core

The engine already models and persists these node behaviours:

- **Memory latch** — set/reset memory, with enable/data support.
- **Advanced clock** — configurable period, high duration and phase offset; it is driven by simulation ticks rather than real time.
- **Edge detector** — emits one tick on a rising input.
- **Pulse generator** — configurable pulse width on a rising input.
- **Delayed pulse** — a rising input emits a pulse after a configurable tick delay.
- **Toggle** — flips its persisted state on each rising input.

The first placeable devices follow Shapez Shifter's building, connector and simulation-system
registration pattern. The stateful/timing devices remain the next milestone because they need
an explicit persisted building-to-node mapping and a configuration UI.

## Save-game safety contract

`OctaneLogicSaveData` is stored in a **separate mod-owned JSON blob** through Shapez Shifter. It is schema-versioned, uses stable node IDs and has no direct references to core-save objects. A future physical building maps its placement to one of these IDs; the logic configuration and runtime state remain isolated from the vanilla save structure.

There is an important platform limit: a save that contains *physical custom buildings* cannot be guaranteed to load, render, simulate, and remain writable in an unmodded game unless Shapez 2 explicitly preserves unknown mod entities as opaque data. No mod can safely promise otherwise. Octane Logic therefore follows these rules:

1. Never mutate or rewrite vanilla building data to encode custom state.
2. Never delete or rewrite unknown nodes during a load/migration failure.
3. Treat an unmodded load as **read-only for Octane Logic data** until game-level opaque-entity preservation is verified.
4. Provide an explicit export/restore workflow before later versions introduce physical mod buildings.

This means toggling the mod off cannot corrupt the base save through Octane Logic's own state. Retaining visible custom gates while the mod is disabled is only possible if the game provides an official placeholder/opaque-entity mechanism; that is a validation gate for the building-integration milestone, not something the mod should fake.

## Local development

1. Install Shapez Shifter's develop build so `SPZ2_SHIFTER` points at its DLL.
2. Set `SPZ2_PATH` to the folder containing the Shapez 2 managed assemblies.
3. Build the deterministic core without the game: `dotnet run --project src/OctaneLogic.Core.Tests`.
4. Build the mod: `dotnet build src/OctaneLogic/OctaneLogic.csproj`.

The output is written to `$(SPZ2_PERSISTENT)/mods/OctaneLogic`.

## Milestones

1. **Core and persistence** — current branch.
2. **API reconnaissance** — identify the supported wire connector, simulation and save compatibility extension points for the currently installed game build.
3. **Stateful physical gates** — add Memory Latch and Advanced Clock buildings with persisted
   configuration, prediction and simulation.
4. **Safety UX** — backup/export command, mod-off warning and compatibility diagnostics.


## Rider / Visual Studio setup

Open **`OctaneLogic.sln`** at the repository root. Do not create or open a separate root `OctaneLogic.csproj`; that project does not contain the required project and Shapez Shifter references.

Before restoring/building, configure these environment variables in your IDE's build/run environment:

- `SPZ2_PATH`: the Shapez 2 managed-assemblies directory (contains `SPZGameAssembly.dll` and `Core.dll`).
- `SPZ2_SHIFTER`: full path to the Shapez Shifter DLL.
- `SPZ2_PERSISTENT`: Shapez 2 persistent-data directory, used only for the build output.

The editor will correctly resolve `OctaneLogic.Core` after loading the canonical solution. The `Core` and `ShapezShifter` references remain unresolved until the first two variables point to the local game/mod installations.

