# Gemini AI Assistant Rules

Please follow these guidelines when working on this project:

## Dependencies and NuGet Packages
* **Do NOT** install new NuGet packages unless explicitly requested or approved by the user. Always ask for permission first before adding any external dependencies.

## Architecture and Layering
* Respect directional dependencies between layers:
  * The **Core** layer should be independent and not know about other layers (it has no outgoing dependencies).
  * The **UI** layer is allowed to know about and depend on other layers.

## UI Development
* The UI is built using **Blazor** and **MudBlazor**.
* Try to utilize as many generic or built-in features of Blazor and MudBlazor as possible before creating custom solutions.

## Build and Testing
* To build the solution, use the `dotnet build` command.
* To run tests, use the `dotnet test --filter "Category!=Integration"` command.

## Workflow and Version Control
* When starting a new session, if you are on the `main` branch, always ask the user if we should stay on the `main` branch or switch to a dedicated branch.

## Feature Documentation
* When adding a new feature, it must be documented in one of the following ways:
  * High-level unit tests on the **Core** layer.
  * A dedicated Product Requirements Document (PRD).
  * A user story or acceptance criteria in dedicated documentation.
* Use the `docs/features/sample-feature` template for documenting features to add.
