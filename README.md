# Octane Logic

Octane Logic is a Shapez 2 logic-expansion mod. Its first release establishes a deterministic, versioned logic engine for stateful gates and timing devices, using Shapez Shifter's supported mod-save-data API.

## What is implemented

The engine already models and persists these node behaviours:

- **Memory latch** — set/reset memory, with enable/data support.
- **Advanced clock** — configurable period, high duration and phase offset; it is driven by simulation ticks rather than real time.
- **Edge detector** — emits one tick on a rising input.
- **Pulse generator** — configurable pulse width on a rising input.
- **Delayed pulse** — a rising input emits a pulse after a configurable tick delay.
- **Toggle** — flips its persisted state on each rising input.

The in-game building, wire-port, renderer and placement integration deliberately comes after the persistence contract. Shapez Shifter supports those pieces, but their concrete wiring types must be verified against the player’s installed Shapez 2 assemblies before binding the API. This keeps the initial implementation honest and prevents a brittle dependency on inferred private game types.

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
3. **Physical gates** — add Memory Latch and Advanced Clock buildings with static assets, prediction and simulation.
4. **Safety UX** — backup/export command, mod-off warning and compatibility diagnostics.

