# TrackYourDay Technical Documentation

This directory contains comprehensive technical documentation for the TrackYourDay application, including architecture overviews, component descriptions, and data flow diagrams.

## Documentation Structure

### 📋 [Architecture Overview](./Architecture-Overview.md)
**Start here** for a high-level understanding of the application.

**Contents**:
- Overall system architecture
- Three-layer design (System, Application, Insights)
- Project structure and namespaces
- Architectural patterns (Event-driven, Repository, Strategy, DI)
- Technology stack
- Design decisions and rationale

**Diagrams**:
- High-level architecture diagram
- Component relationships

### 🖥️ [System Trackers](./System-Trackers.md)
Low-level system event tracking and activity recognition.

**Contents**:
- ActivityTracker component
- Activity types (Started, Ended, Instant)
- System states (Focus, Mouse, Input, Lock)
- Recognition strategies
- Activity flow and state transitions
- Events published by system trackers

**Diagrams**:
- ActivityTracker class diagram
- System state hierarchy
- Activity recognition sequence diagram

### 📱 [Application Trackers](./Application-Trackers.md)
Domain-specific tracking including breaks, meetings, and external integrations.

**Contents**:
- Break Tracker (inactivity detection, break management)
- MS Teams Meeting Tracker (meeting detection)
- Jira Tracker (issue tracking, time logging)
- GitLab Tracker (commit tracking)
- User Tasks Tracker (manual entries)
- Break revocation workflow

**Diagrams**:
- Break detection state machine
- Meeting discovery sequence
- Jira integration architecture
- Break processing flow

### 📊 [Insights](./Insights.md)
High-level analysis, workday management, and activity summarization.

**Contents**:
- Workday management (metrics, calculations)
- WorkdayDefinition and configuration
- Analytics and summarization strategies
  - Duration-based
  - Context-based
  - Hybrid contextual
  - Jira-enriched
  - ML-based
- Notification system
- Read model architecture

**Diagrams**:
- Workday class diagram
- Metric calculation flow
- Summary strategy comparison

### 🔄 [Data Flow](./Data-Flow.md)
How data moves through the system from events to insights.

**Contents**:
- Complete data flow overview
- Detailed flow scenarios:
  - Application switching
  - Break detection and end
  - Jira integration
  - System lock handling
  - End-of-day summary
- Background job scheduling
- Event flow patterns
- Error handling strategies

**Diagrams**:
- End-to-end data flow
- Scenario-specific sequence diagrams
- Job scheduling timeline
- Event flow patterns

### 🔗 [Dependencies and Integrations](./Dependencies-and-Integrations.md)
External dependencies, package usage, and integration details.

**Contents**:
- External dependencies (NuGet packages)
  - MediatR, ML.NET, Serilog, Quartz.NET, etc.
- Internal component dependencies
- External integrations:
  - Jira REST API integration
  - GitLab REST API integration
  - MS Teams process detection
- Database schema (SQLite)
- Configuration management
- Security considerations

**Diagrams**:
- Component dependency graph
- Layer dependencies
- Integration data flows
- Database schema

## Quick Reference

### Common Questions

**Q: Where do I start?**  
A: Begin with [Architecture Overview](./Architecture-Overview.md) to understand the overall system design.

**Q: How are activities detected?**  
A: See [System Trackers](./System-Trackers.md) → Activity Recognition Flow section.

**Q: How are breaks calculated?**  
A: See [Application Trackers](./Application-Trackers.md) → Break Tracker → Algorithm section.

**Q: How does Jira integration work?**  
A: See [Dependencies and Integrations](./Dependencies-and-Integrations.md) → Jira Integration section.

**Q: How is the workday calculated?**  
A: See [Insights](./Insights.md) → Workday Management → Calculation Flow section.

**Q: What happens when I switch applications?**  
A: See [Data Flow](./Data-Flow.md) → Scenario 1: User Switches Application.

### Key Components Quick Links

| Component | Documentation | Purpose |
|-----------|--------------|---------|
| `ActivityTracker` | [System Trackers](./System-Trackers.md#activitytracker) | Detects system-level activities |
| `BreakTracker` | [Application Trackers](./Application-Trackers.md#break-tracker) | Detects and manages breaks |
| `Workday` | [Insights](./Insights.md#workday) | Manages workday metrics |
| `JiraTracker` | [Application Trackers](./Application-Trackers.md#jira-tracker) | Jira integration |
| `ActivitiesAnalyser` | [Insights](./Insights.md#activitiesanalyser) | Generates activity summaries |

### Architecture Patterns Quick Links

| Pattern | Documentation | Usage |
|---------|--------------|-------|
| Event-Driven | [Architecture Overview](./Architecture-Overview.md#event-driven-architecture) | MediatR-based event system |
| Repository | [Architecture Overview](./Architecture-Overview.md#repository-pattern) | Data persistence abstraction |
| Strategy | [Architecture Overview](./Architecture-Overview.md#strategy-pattern) | Pluggable summarization |
| DI | [Architecture Overview](./Architecture-Overview.md#dependency-injection) | Service registration |

## Mermaid Diagrams

All documentation uses [Mermaid](https://mermaid.js.org/) for diagrams. These render automatically on GitHub and most Markdown viewers.

**Types of diagrams used**:
- **Flowcharts**: Data flow, process flows
- **Sequence diagrams**: Component interactions over time
- **Class diagrams**: Object relationships and structures
- **State diagrams**: State machines and transitions
- **Gantt charts**: Job scheduling timelines
- **Graph diagrams**: Dependencies and relationships

## Contributing to Documentation

When adding or updating documentation:

1. **Keep it concrete**: Use specific examples and code snippets
2. **Add diagrams**: Visual representations help understanding
3. **Link between docs**: Cross-reference related sections
4. **Update this README**: Add new documents to the structure above
5. **Follow the style**: Match the tone and format of existing docs

### Documentation Style Guide

- Use **bold** for emphasis
- Use `code blocks` for class/method names
- Use > blockquotes for important notes
- Number lists for sequential steps
- Bullet lists for unordered items
- Keep paragraphs concise (3-5 sentences)
- Add section headers for scanability

## Related Resources

- [Main README](../README.md): User-facing documentation
- [License](../LICENSE.md): BSD-style license
- [Issues](https://github.com/Skuty/TrackYourDay/issues): Bug reports and feature requests
- [Releases](https://github.com/Skuty/TrackYourDay/releases): Download latest version

## Document Metadata

| Document | Last Updated | Status |
|----------|-------------|---------|
| Architecture Overview | 2024-12-06 | ✅ Complete |
| System Trackers | 2024-12-06 | ✅ Complete |
| Application Trackers | 2024-12-06 | ✅ Complete |
| Insights | 2024-12-06 | ✅ Complete |
| Data Flow | 2024-12-06 | ✅ Complete |
| Dependencies | 2024-12-06 | ✅ Complete |

---

**Note**: This documentation describes the architecture as of December 2024. For the most up-to-date code, refer to the source files in the `src/` directory.
