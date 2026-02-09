# Building a Multi-Agent AI System with A2A, MCP, and ADK in .NET

> How we combined three open AI protocols — Google's A2A & ADK with Anthropic's MCP — to build a production-ready Multi-Agent Research Assistant using .NET 10.

---

## Introduction

The AI landscape is evolving rapidly. We've moved beyond single LLM calls and are now entering the era of **multi-agent systems** — where specialized AI agents collaborate like a team of experts to solve complex problems.

But here's the challenge: **How do you make agents talk to each other? How do you give them tools? How do you orchestrate them?**

Three open protocols have emerged to answer these questions:

- **MCP (Model Context Protocol)** by Anthropic — The "USB-C for AI"
- **A2A (Agent-to-Agent Protocol)** by Google — The "phone line between agents"
- **ADK (Agent Development Kit)** by Google — The "organizational chart for agents"

In this article, I'll explain each protocol, show how they complement each other, and walk through our real-world project: a **Multi-Agent Research Assistant** built with ABP Framework.

<!-- 
📸 IMAGE 1: Hero Banner
Create a visually appealing banner showing three protocol logos (MCP, A2A, ADK) 
converging into a .NET logo. Include text: "Multi-Agent AI System"
Style: Modern, dark background with gradient accents (blue, purple, green)
Dimensions: 1200x630px (social share friendly)
-->

---

## The Problem: Why Single-Agent Isn't Enough

Imagine you ask an AI: *"Research the latest AI agent frameworks and give me a comprehensive analysis report."*

A single LLM call would:
- ❌ Hallucinate search results (can't actually browse the web)
- ❌ Produce a shallow analysis (no structured research pipeline)
- ❌ Lose context between steps (no state management)
- ❌ Can't save results anywhere (no tool access)

What you actually need is a **team of specialists**:

1. A **Researcher** who searches the web and gathers raw data
2. An **Analyst** who processes that data into a structured report
3. **Tools** that let agents interact with the real world (web, database, filesystem)
4. An **Orchestrator** that coordinates everything

This is exactly what we built.

<!-- 
📸 IMAGE 2: Single Agent vs Multi-Agent Comparison
Left side: Single Agent (one robot trying to do everything, overwhelmed)
Right side: Multi-Agent Team (Researcher + Analyst + Tools working together)
Show arrows connecting them in a pipeline
Style: Clean infographic, two-panel comparison
Dimensions: 1200x500px
-->

---

## Protocol #1: MCP — Giving Agents Superpowers

### What is MCP?

**MCP (Model Context Protocol)**, created by Anthropic, is a standardized way to connect AI models to external tools and data sources. Think of it as **USB-C for AI** — one universal connector that works with everything.

Before MCP, if you wanted your LLM to search the web, query a database, and save files, you had to write custom integration code for each capability. With MCP, you define tools once and any MCP-compatible agent can use them.

<!-- 
📸 IMAGE 3: MCP as USB-C Analogy
Left panel: "Before MCP" — Multiple different cables/connectors tangled together
  (Custom API for web search, custom API for database, custom API for files...)
Right panel: "With MCP" — One clean USB-C cable connecting everything
  (MCP standard connecting LLM to Web Search, Database, File System)
Style: Simple, clean analogy illustration
Dimensions: 1200x400px
-->

### How MCP Works

MCP follows a simple **Client-Server architecture**:

```
┌─────────────────┐         ┌─────────────────┐
│   MCP Client    │         │   MCP Server    │
│   (The Agent)   │ ◄─────► │   (The Tools)   │
│                 │         │                 │
│  "What tools    │ ──GET── │  "Here's what   │
│   do you have?" │         │   I can do"     │
│                 │         │                 │
│  "Search for X" │ ─CALL─► │  *searches web* │
│                 │ ◄─────  │  "Here's the    │
│                 │         │   results"      │
└─────────────────┘         └─────────────────┘
```

The flow is straightforward:

1. **Discovery**: The agent asks "What tools do you have?" (`tools/list`)
2. **Invocation**: The agent calls a specific tool (`tools/call`)
3. **Result**: The tool returns data back to the agent

### MCP in Our Project

We built three MCP tool servers:

| MCP Tool | Purpose | Used By |
|----------|---------|---------|
| `web_search` | Searches the web via Tavily API | Researcher Agent |
| `fetch_url_content` | Fetches content from a URL | Researcher Agent |
| `save_research_to_file` | Saves reports to the filesystem | Analysis Agent |
| `save_research_to_database` | Persists results in SQL Server | Analysis Agent |
| `search_past_research` | Queries historical research | Analysis Agent |

The beauty of MCP is that agents don't need to know *how* these tools work internally. They just see a description and call them by name. The tool handles the rest.

<!-- 
📸 IMAGE 4: MCP Tool Architecture in Our Project
Show a central "MCP Server" box with three tool groups radiating outward:
  - Web Search Tools (globe icon): web_search, fetch_url_content
  - File System Tools (folder icon): save_research_to_file, read_research_file, list_research_files
  - Database Tools (database icon): save_research_to_database, search_past_research, get_recent_research
Each tool group connects to its data source (Internet, File System, SQL Server)
Two agents shown on the left connecting to the MCP Server
Style: Architecture diagram with icons, clean lines
Dimensions: 1200x600px
-->

---

## Protocol #2: A2A — Making Agents Talk to Each Other

### What is A2A?

**A2A (Agent-to-Agent)**, originally proposed by Google and now under the Linux Foundation, is a protocol that lets AI agents **discover each other and exchange tasks**. If MCP is about giving agents tools, A2A is about giving agents the ability to communicate.

Think of it this way:
- **MCP** = "What can this agent *do*?" (capabilities)
- **A2A** = "How do agents *talk*?" (communication)

### The Agent Card: Your Agent's Business Card

Every A2A-compatible agent publishes an **Agent Card** — a JSON document that describes who it is and what it can do. It's like a business card for AI agents:

```json
{
  "name": "Researcher Agent",
  "description": "Searches the web to collect comprehensive research data",
  "url": "https://localhost:44331/a2a/researcher",
  "version": "1.0.0",
  "capabilities": {
    "streaming": false,
    "pushNotifications": false
  },
  "skills": [
    {
      "id": "web-research",
      "name": "Web Research",
      "description": "Searches the web on a given topic and collects raw data",
      "tags": ["research", "web-search", "data-collection"]
    }
  ]
}
```

Other agents can discover this card at `/.well-known/agent.json` and immediately know:
- What this agent does
- Where to reach it
- What skills it has

<!-- 
📸 IMAGE 5: A2A Agent Discovery Flow
Show three steps as a horizontal flow:
  Step 1: "Discovery" — Agent A sends GET request to Agent B's /.well-known/agent.json
  Step 2: "Task Sending" — Agent A sends POST /tasks/send with a task payload
  Step 3: "Result" — Agent B returns completed task with artifacts
Use icons: magnifying glass (discovery), envelope (task), checkmark (result)
Show Agent Card JSON preview in Step 1
Style: Step-by-step flow diagram with numbered circles
Dimensions: 1200x400px
-->

### How A2A Task Exchange Works

Once an agent discovers another agent, it can send tasks:

```
Orchestrator                         Researcher Agent
     │                                      │
     │  1. GET /.well-known/agent.json      │
     │ ────────────────────────────────────► │
     │  ◄── Agent Card (skills, URL)        │
     │                                      │
     │  2. POST /tasks/send                 │
     │     { "Research AI frameworks" }      │
     │ ────────────────────────────────────► │
     │                                      │ 🔍 Searching web...
     │                                      │ 📝 Collecting data...
     │                                      │
     │  3. ◄── { status: "completed",       │
     │           artifacts: [report] }       │
     │                                      │
```

The key concepts:

- **Task**: A unit of work sent between agents (like an email with instructions)
- **Artifact**: The output produced by an agent (like an attachment in the reply)
- **Task State**: `Submitted → Working → Completed/Failed`

### A2A in Our Project

Our system uses A2A for inter-agent communication:

- The **Orchestrator** discovers both agents via their Agent Cards
- It sends a research task to the **Researcher Agent**
- The Researcher's output (artifacts) becomes input for the **Analysis Agent**
- The Analysis Agent produces the final structured report

<!-- 
📸 IMAGE 6: A2A Communication in Our Project
Show a sequence diagram with three actors:
  - Orchestrator (center, larger)
  - Researcher Agent (left)
  - Analysis Agent (right)
Flow:
  1. Orchestrator → Researcher: "Research this topic"
  2. Researcher → Orchestrator: Returns raw research data
  3. Orchestrator → Analyst: "Analyze this data" (passes research from step 2)
  4. Analyst → Orchestrator: Returns structured analysis report
Show task states (Working → Completed) at each step
Style: UML sequence diagram, colored actors
Dimensions: 1000x600px
-->

---

## Protocol #3: ADK — Organizing Your Agent Team

### What is ADK?

**ADK (Agent Development Kit)**, created by Google, provides patterns for **organizing and orchestrating multiple agents**. It answers the question: "How do you build a team of agents that work together efficiently?"

ADK gives you:
- **BaseAgent**: A foundation every agent inherits from
- **SequentialAgent**: Runs agents one after another (pipeline)
- **ParallelAgent**: Runs agents simultaneously
- **AgentContext**: Shared state that flows through the pipeline
- **AgentEvent**: Control flow signals (escalate, transfer, state updates)

> **Note**: ADK's official SDK is Python-only. We ported the core patterns to .NET for our project.

### The Pipeline Pattern

The most powerful ADK pattern is the **Sequential Pipeline**. Think of it as an assembly line in a factory:

```
┌──────────┐    State    ┌──────────┐    State    ┌──────────┐
│          │   flows     │          │   flows     │          │
│ Agent A  │ ──────────► │ Agent B  │ ──────────► │ Agent C  │
│          │             │          │             │          │
│ Produces │             │ Consumes │             │ Consumes │
│ output   │             │ A's data │             │ B's data │
│          │             │ Produces │             │ Produces │
│          │             │ output   │             │ final    │
└──────────┘             └──────────┘             └──────────┘
```

Each agent:
1. Receives the shared **AgentContext** (with state from previous agents)
2. Does its work
3. Updates the state
4. Passes it to the next agent

<!-- 
📸 IMAGE 7: ADK Sequential Pipeline — Factory Assembly Line Analogy
Top: Real-world analogy — Factory assembly line with stations
  Station 1: "Raw Materials" → Station 2: "Processing" → Station 3: "Quality Check" → Final Product
Bottom: Agent pipeline mapped to it
  Researcher Agent → Analysis Agent → (Output: Structured Report)
Show AgentContext as a conveyor belt carrying state between agents
State grows at each step: {query} → {query, research_data} → {query, research_data, analysis_report}
Style: Infographic with assembly line metaphor, clear mapping
Dimensions: 1200x500px
-->

### AgentContext: The Shared Memory

`AgentContext` is like a shared whiteboard that all agents can read from and write to:

```
AgentContext
┌────────────────────────────────────────────┐
│  UserQuery: "AI agent frameworks 2026"     │
│                                            │
│  State:                                    │
│  ├─ researcher_result: "Raw data..."       │  ← Written by Researcher
│  ├─ researcher_status: "completed"         │  ← Written by Researcher
│  ├─ analyst_result: "# Analysis..."        │  ← Written by Analyst
│  └─ analyst_status: "completed"            │  ← Written by Analyst
│                                            │
│  Events:                                   │
│  ├─ [14:30:01] Researcher started          │
│  ├─ [14:30:05] Web search completed        │
│  ├─ [14:30:06] Researcher completed        │
│  ├─ [14:30:06] Analyst started             │
│  ├─ [14:30:12] Analysis completed          │
│  └─ [14:30:12] Pipeline finished           │
└────────────────────────────────────────────┘
```

This pattern eliminates the need for complex inter-agent messaging — agents simply read and write to a shared context.

### ADK Orchestration Patterns

ADK supports multiple orchestration patterns:

<!-- 
📸 IMAGE 8: ADK Orchestration Patterns (4-panel grid)
Panel 1: "Sequential Pipeline" — A → B → C (linear flow)
Panel 2: "Parallel Execution" — A, B, C running simultaneously, results merged
Panel 3: "Fan-Out / Fan-In" — One input splits to A, B, C then merges back
Panel 4: "Conditional Routing" — Decision diamond routing to A or B based on condition
Each panel should be a simple, clear diagram with labeled arrows
Style: 2x2 grid of mini-diagrams, consistent color scheme
Dimensions: 1200x800px
-->

| Pattern | Description | Use Case |
|---------|-------------|----------|
| **Sequential** | A → B → C | Research → Analysis pipeline |
| **Parallel** | A, B, C simultaneously | Multiple searches at once |
| **Fan-Out/Fan-In** | Split → Process → Merge | Distributed research |
| **Conditional Routing** | If/else agent selection | Route by query type |

---

## How the Three Protocols Work Together

Here's the key insight: **MCP, A2A, and ADK are not competitors — they're complementary layers of a complete agent system.**

```
┌─────────────────────────────────────────────────────┐
│                AGENT ECOSYSTEM                       │
│                                                      │
│   ┌─── ADK Layer (Orchestration) ──────────────┐    │
│   │                                             │    │
│   │   SequentialAgent                           │    │
│   │   ┌──────────┐      ┌──────────┐          │    │
│   │   │Researcher│ ───► │ Analyst  │           │    │
│   │   │ Agent    │      │ Agent    │           │    │
│   │   └────┬─────┘      └────┬─────┘          │    │
│   │        │                  │                 │    │
│   └────────┼──────────────────┼─────────────────┘    │
│            │                  │                       │
│   ┌────────┼── A2A Layer (Communication) ──────┐    │
│   │        │                  │                 │    │
│   │   Agent Card         Agent Card            │    │
│   │   Task Exchange      Task Exchange         │    │
│   │        │                  │                 │    │
│   └────────┼──────────────────┼─────────────────┘    │
│            │                  │                       │
│   ┌────────┼── MCP Layer (Tools) ──────────────┐    │
│   │        │                  │                 │    │
│   │   ┌────▼────┐      ┌─────▼─────┐          │    │
│   │   │Web Search│     │ File Save  │          │    │
│   │   │URL Fetch │     │ DB Save    │          │    │
│   │   └─────────┘     │ DB Query   │          │    │
│   │                    └───────────┘          │    │
│   └─────────────────────────────────────────────┘    │
│                                                      │
└─────────────────────────────────────────────────────┘
```

Each protocol handles a different concern:

| Layer | Protocol | Question It Answers |
|-------|----------|-------------------|
| **Top** | ADK | "How are agents organized?" |
| **Middle** | A2A | "How do agents communicate?" |
| **Bottom** | MCP | "What tools can agents use?" |

<!-- 
📸 IMAGE 9: Three-Layer Protocol Stack (Main Architecture Diagram)
Create a visually polished 3-layer stack diagram:
  Layer 3 (Top, Blue): ADK — Orchestration Layer
    Contains: SequentialAgent orchestrating Researcher → Analyst pipeline
  Layer 2 (Middle, Green): A2A — Communication Layer
    Contains: Agent Cards, Task Exchange arrows between agents
  Layer 1 (Bottom, Purple): MCP — Tool Layer
    Contains: Web Search, File System, Database tools with their icons
  
Outside the stack: User/API on the left sending a query
Arrow from User → through all 3 layers → back to User with result

This is the MOST IMPORTANT image of the article. Make it professional and clear.
Style: Modern tech architecture diagram, rounded rectangles, gradient colors
Dimensions: 1200x700px
-->

---

## Our Project: Multi-Agent Research Assistant

### Built With

- **.NET 10.0** — Latest runtime
- **ABP Framework 10.0.2** — Enterprise .NET application framework
- **Semantic Kernel 1.70.0** — Microsoft's AI orchestration SDK
- **Azure OpenAI (GPT)** — LLM backbone
- **Tavily Search API** — Real-time web search
- **SQL Server** — Research persistence
- **MCP SDK** (`ModelContextProtocol` 0.8.0-preview.1)
- **A2A SDK** (`A2A` 0.3.3-preview)

### Architecture Overview

Our system processes a user's research query through a multi-agent pipeline:

<!-- 
📸 IMAGE 10: Complete System Architecture — End-to-End Flow
Create a detailed but clean architecture diagram showing:

Left side: User Interface (Dashboard)
  ↓ HTTP POST /api/app/research/execute
  
Center: ABP Framework Application
  ├── ResearchAppService (API Layer)
  │     ↓
  ├── ResearchOrchestrator (ADK SequentialAgent)
  │     ├── Mode 1: ADK Sequential Pipeline
  │     └── Mode 2: A2A Protocol-Based
  │     ↓
  ├── Researcher Agent (GPT + MCP Tools)
  │     ├── web_search (Tavily API) → Internet
  │     └── fetch_url_content → Web Pages
  │     ↓ (state transfer: research_result)
  ├── Analysis Agent (GPT + MCP Tools)
  │     ├── save_research_to_file → File System
  │     └── save_research_to_database → SQL Server
  │     ↓
  └── Final Result (ResearchResultDto)
        ↓
Right side: Dashboard displays results (Research Report + Analysis Report)

Style: Professional architecture diagram, left-to-right or top-to-bottom flow
Dimensions: 1200x800px
-->

### How It Works (Step by Step)

**Step 1: User Submits a Query**

The user enters a research topic in the dashboard — for example, *"Compare the latest AI agent frameworks: LangChain, Semantic Kernel, and AutoGen"* — and selects an execution mode (ADK Sequential or A2A).

**Step 2: Orchestrator Activates**

The `ResearchOrchestrator` receives the query and creates an `AgentContext`. In ADK mode, it sets up a `SequentialAgent` with two sub-agents. In A2A mode, it sends tasks via the `A2AServer`.

**Step 3: Researcher Agent Goes to Work**

The Researcher Agent:
- Receives the query from the context
- Uses GPT to formulate optimal search queries
- Calls the `web_search` MCP tool (powered by Tavily API)
- Collects and synthesizes raw research data
- Stores results in the shared `AgentContext`

**Step 4: Analysis Agent Takes Over**

The Analysis Agent:
- Reads the Researcher's raw data from `AgentContext`
- Uses GPT to perform deep analysis
- Generates a structured Markdown report with sections:
  - Executive Summary
  - Key Findings
  - Detailed Analysis
  - Comparative Assessment
  - Conclusion and Recommendations
- Calls MCP tools to save the report to both filesystem and database

**Step 5: Results Returned**

The orchestrator collects all results and returns them to the user via the REST API. The dashboard displays the research report, analysis report, agent event timeline, and raw data.

<!-- 
📸 IMAGE 11: Step-by-Step Pipeline Flow (Visual Timeline)
Create a horizontal timeline/pipeline showing 5 steps:

Step 1: 🔍 "User Query" 
  → "Compare AI agent frameworks"

Step 2: 🎯 "Orchestrator"
  → Creates pipeline, selects mode

Step 3: 🌐 "Researcher Agent" 
  → GPT + web_search MCP tool
  → Output: Raw research data (shown as a data card)

Step 4: 📊 "Analysis Agent"
  → GPT + file_save + db_save MCP tools
  → Output: Structured report (shown as a report card)

Step 5: ✅ "Result"
  → Dashboard shows complete research

Connect steps with arrows, show state flowing between steps
Style: Modern process flow, numbered circles, icon-rich
Dimensions: 1200x400px
-->

### Two Execution Modes

Our system supports two execution modes, demonstrating both ADK and A2A approaches:

#### Mode 1: ADK Sequential Pipeline

Agents are organized as a `SequentialAgent`. State flows automatically through the pipeline via `AgentContext`. This is an in-process approach — fast and simple.

```
SequentialAgent
├── Step 1: ResearcherAgent.RunAsync(context)
│   └── Writes: context.State["researcher_result"] = rawData
│
├── Step 2: AnalysisAgent.RunAsync(context)
│   └── Reads: context.State["researcher_result"]
│   └── Writes: context.State["analyst_result"] = report
│
└── Return: Aggregated results from context
```

#### Mode 2: A2A Protocol-Based

Agents communicate via the A2A protocol. The Orchestrator sends `AgentTask` objects to each agent through the `A2AServer`. Each agent has its own `AgentCard` for discovery.

```
Orchestrator
├── Step 1: a2aServer.HandleTaskAsync("researcher", task)
│   └── Returns: AgentTask with Artifacts
│
├── Step 2: a2aServer.HandleTaskAsync("analyst", task)
│   └── Input includes Researcher's artifacts
│   └── Returns: AgentTask with final Artifacts
│
└── Return: Extracted results from Artifacts
```

<!-- 
📸 IMAGE 12: Two Execution Modes — Side-by-Side Comparison
Left panel: "ADK Sequential Mode"
  - Show SequentialAgent wrapping both agents
  - AgentContext flowing as a shared state object (like a clipboard being passed)
  - Label: "In-Process, Shared Memory"
  
Right panel: "A2A Protocol Mode"
  - Show A2AServer in the middle
  - Researcher and Analyst as separate services
  - AgentTask objects being sent as messages (like envelopes)
  - Agent Cards shown next to each agent
  - Label: "Protocol-Based, Message Passing"

Both panels show same input/output but different internal mechanics
Style: Two-panel comparison diagram, visually distinct modes
Dimensions: 1200x500px
-->

### The Dashboard

The UI provides a complete research experience:

- **Hero Section** with system description and protocol badges
- **Architecture Cards** showing all four components (Researcher, Analyst, MCP Tools, Orchestrator)
- **Research Form** with query input and mode selection
- **Live Pipeline Status** tracking each stage of execution
- **Tabbed Results** view: Research Report, Analysis Report, Raw Data, Agent Events
- **Research History** table with past queries and their results

<!-- 
📸 IMAGE 13: Dashboard Screenshot
Take a screenshot of the actual running dashboard, or create a mockup showing:
- Header with "Multi-Agent Research Assistant" title
- Four architecture cards in a row (Researcher, Analyst, MCP Tools, Orchestrator)
- Research form with a sample query filled in
- Results section with tabs showing a sample analysis report
- History table with a few entries
Style: Actual screenshot or high-fidelity mockup
Dimensions: 1200x900px (full page capture)
-->

---

## Why ABP Framework?

We chose ABP Framework as our .NET application foundation. Here's why it was a natural fit:

| ABP Feature | How We Used It |
|-------------|---------------|
| **Auto API Controllers** | `ResearchAppService` automatically becomes REST API endpoints |
| **Dependency Injection** | Clean registration of agents, tools, orchestrator, Semantic Kernel |
| **Repository Pattern** | `IRepository<ResearchRecord>` for database operations in MCP tools |
| **Module System** | All agent ecosystem config encapsulated in `AgentEcosystemModule` |
| **Entity Framework Core** | Research record persistence with code-first migrations |
| **Built-in Auth** | OpenIddict integration for securing agent endpoints |
| **Health Checks** | Monitoring agent ecosystem health |

ABP's single-layer template gave us the perfect .NET foundation — all the enterprise features without unnecessary complexity for a focused AI project. That said, the agent architecture (MCP, A2A, ADK) is framework-agnostic and works with any .NET application.

---

## Key Takeaways

### 1. Protocols Are Complementary, Not Competing

MCP, A2A, and ADK solve different problems. Using them together creates a complete agent system:
- **MCP**: Standardize tool access
- **A2A**: Standardize inter-agent communication
- **ADK**: Standardize agent orchestration

### 2. Start Simple, Scale Later

Our project runs everything in a single process (in-process A2A). But because we used the A2A protocol, each agent can be extracted into its own microservice later — without changing the core logic.

### 3. Shared State > Message Passing (For Simple Cases)

ADK's `AgentContext` with shared state is simpler and faster than A2A message passing for in-process scenarios. Use A2A when agents need to run as separate services.

### 4. MCP is the Real Game-Changer

The ability to define tools once and have any agent use them — with automatic discovery and structured invocations — eliminates enormous amounts of boilerplate code.

### 5. LLM Abstraction is Critical

Using Semantic Kernel's `IChatCompletionService` lets you swap between Azure OpenAI, OpenAI, Ollama, or any provider without touching agent code.

<!-- 
📸 IMAGE 14: Key Takeaways — Visual Summary
Create an infographic with 5 takeaway cards arranged in a grid or list:
  1. 🔗 "Complementary Protocols" — Three interlocking puzzle pieces (MCP, A2A, ADK)
  2. 📈 "Start Simple, Scale Later" — Small box → Large distributed system
  3. 📋 "Shared State Pattern" — Clipboard/whiteboard metaphor
  4. 🔌 "MCP Game-Changer" — USB-C plugging into multiple tools
  5. 🔄 "LLM Abstraction" — Swap icon between OpenAI/Azure/Ollama logos
Style: Icon-rich takeaway cards, clean and modern
Dimensions: 1200x600px
-->

---

## What's Next?

This project demonstrates the foundation of a multi-agent system. Future enhancements could include:

- **Streaming responses** — Real-time updates as agents work (A2A supports this)
- **More specialized agents** — Code analysis, translation, fact-checking agents
- **Distributed deployment** — Each agent as a separate microservice with HTTP-based A2A
- **Agent marketplace** — Discover and integrate third-party agents via A2A Agent Cards
- **Human-in-the-loop** — Using A2A's `InputRequired` state for human approval steps
- **RAG integration** — MCP tools for vector database search

---

## Resources

| Resource | Link |
|----------|------|
| **MCP Specification** | [modelcontextprotocol.io](https://modelcontextprotocol.io) |
| **A2A Specification** | [google.github.io/A2A](https://google.github.io/A2A) |
| **ADK Documentation** | [google.github.io/adk-docs](https://google.github.io/adk-docs) |
| **ABP Framework** | [abp.io](https://abp.io) |
| **Semantic Kernel** | [github.com/microsoft/semantic-kernel](https://github.com/microsoft/semantic-kernel) |
| **MCP .NET SDK** | [NuGet: ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol) |
| **A2A .NET SDK** | [NuGet: A2A](https://www.nuget.org/packages/A2A) |
| **Our Source Code** | [GitHub Repository](#) |

---

## Conclusion

Building a multi-agent AI system is no longer a futuristic concept — it's achievable today with open protocols and modern frameworks. By combining **MCP** for tool access, **A2A** for agent communication, and **ADK** for orchestration, we created a Research Assistant that demonstrates real-world multi-agent collaboration.

ABP Framework and .NET proved to be an excellent foundation, providing the enterprise infrastructure (DI, repositories, auto APIs, modules) that let us focus entirely on the AI agent architecture.

The era of single LLM calls is ending. The era of **agent ecosystems** has begun.

<!-- 
📸 IMAGE 15: Closing Banner
Create a visually impactful closing image showing:
- A network of connected agents (nodes and edges)
- Three protocol badges: MCP ✓, A2A ✓, ADK ✓
- .NET logo
- Text: "The Era of Agent Ecosystems"
Style: Dark background, glowing connections, futuristic feel
Dimensions: 1200x400px
-->

---

*This article is part of the Agent Ecosystem project built with .NET 10.0, Semantic Kernel 1.70.0, Azure OpenAI, and ABP Framework 10.0.2.*

*If you have questions or want to discuss multi-agent architectures, feel free to reach out in the comments below!*
