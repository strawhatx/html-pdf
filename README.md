PdfApi.sln
├── src/
│   ├── PdfApi.Api                 ← the API you already have
│   ├── PdfApi.Renderer             ← the worker plane
│   ├── PdfApi.Core                 ← shared domain/contracts
│   ├── PdfApi.Data                 ← Postgres/EF Core layer
│   ├── PdfApi.Queue                ← queue abstraction
│   └── PdfApi.BreakTokenDiagnostics (optional, see below)
├── tests/
│   ├── PdfApi.Api.Tests
│   ├── PdfApi.Renderer.Tests
│   └── PdfApi.FixtureSuite.Tests   ← runs the 8 benchmark fixtures
└── deploy/
    ├── PdfApi.Api.Dockerfile
    └── PdfApi.Renderer.Dockerfile