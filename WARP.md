# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## About Prowlarr

Prowlarr is an indexer manager/proxy built on the popular *arr .net/reactjs base stack to integrate with various PVR apps. It supports management of both Torrent Trackers and Usenet Indexers with seamless integration to Lidarr, Mylar3, Radarr, Readarr, and Sonarr.

## Development Commands

### Build Commands

```bash
# Build everything (backend + frontend + packages + lint)
./build.sh

# Build only backend
./build.sh --backend

# Build only frontend
./build.sh --frontend

# Build packages
./build.sh --packages

# Build all components
./build.sh --all

# Build with specific runtime and framework
./build.sh --backend -r linux-x64 -f net8.0

# Enable extra platforms (FreeBSD)
./build.sh --backend --enable-extra-platforms

# Create Windows installer
./build.sh --installer
```

### Frontend Development

```bash
# Install dependencies
yarn install

# Development build with watch
yarn start
# or
yarn watch

# Production build
yarn build

# Clean build artifacts
yarn clean
```

### Linting

```bash
# Run all linting
./build.sh --lint

# Frontend linting only
yarn lint
yarn lint-fix

# Style linting (Linux/macOS)
yarn stylelint-linux

# Style linting (Windows)
yarn stylelint-windows
```

### Testing

```bash
# Run unit tests (specify platform: Windows, Linux, Mac)
./test.sh Linux Unit Test

# Run integration tests
./test.sh Linux Integration Test

# Run automation tests
./test.sh Linux Automation Test

# Run with coverage
./test.sh Linux Unit Coverage
```

### Single Test Commands

```bash
# Run specific test assembly
dotnet test _tests/Prowlarr.Core.Test.dll --filter "Category!=ManualTest"

# Run tests for specific category
dotnet test _tests/Prowlarr.Core.Test.dll --filter "Category!=IntegrationTest&Category!=AutomationTest"
```

## High-Level Architecture

### Project Structure

Prowlarr follows a layered .NET Core architecture with a React frontend:

#### Backend (.NET 8.0)

- **NzbDrone (Prowlarr)**: Main executable entry point
- **NzbDrone.Host (Prowlarr.Host)**: Application hosting and bootstrapping
- **NzbDrone.Core (Prowlarr.Core)**: Business logic and core functionality
- **NzbDrone.Common (Prowlarr.Common)**: Shared utilities and infrastructure
- **Prowlarr.Api.V1**: REST API controllers and models
- **Prowlarr.Http**: HTTP handling, middleware, and web infrastructure

#### Core Domain Areas

Within `NzbDrone.Core`, the main functional areas include:

- **Indexers**: Torrent and Usenet indexer management and definitions
- **Applications**: Integration with *arr applications (Sonarr, Radarr, etc.)
- **DownloadClients**: Integration with download client software
- **IndexerSearch**: Search functionality across indexers
- **History**: Search and grab history tracking
- **Authentication**: User authentication and authorization
- **Configuration**: Application settings and preferences
- **Notifications**: Webhook and notification integrations
- **HealthCheck**: System health monitoring
- **Datastore**: Database abstraction and migrations

#### Frontend (React/TypeScript)

Located in `/frontend/src/`:

- **Components**: Reusable UI components
- **App**: Main application shell and routing
- **Settings**: Configuration pages
- **Indexer**: Indexer management interfaces
- **Search**: Search functionality UI
- **History**: Search history views
- **Store**: Redux state management
- **Utilities**: Frontend utilities and helpers

#### Platform Abstractions

- **NzbDrone.Mono (Prowlarr.Mono)**: Unix/Linux platform-specific code
- **NzbDrone.Windows (Prowlarr.Windows)**: Windows platform-specific code

#### Testing

- **Unit Tests**: `*.Test.dll` assemblies for each main project
- **Integration Tests**: End-to-end testing with real indexers
- **Automation Tests**: UI automation tests

### Key Architectural Patterns

- **Dependency Injection**: Uses built-in .NET DI container
- **Repository Pattern**: Data access abstraction via `IBasicRepository<T>`
- **Provider Pattern**: Pluggable providers for indexers, download clients, etc.
- **Event-Driven**: Messaging system for decoupled communication
- **Command/Query Separation**: Separate command and query handlers

### Build System

- **Backend**: MSBuild with cross-platform support (Windows, Linux, macOS, FreeBSD)
- **Frontend**: Webpack-based build system with TypeScript
- **CI/CD**: Azure Pipelines with comprehensive testing across platforms
- **Packaging**: Creates platform-specific packages and installers

### Database Support

- SQLite (default)
- PostgreSQL (optional)
- SQL Server (legacy support)

## Development Environment

### Prerequisites

- .NET 8.0 SDK
- Node.js 20.x
- Yarn package manager

### First Time Setup

```bash
# Install .NET dependencies (automatic via build script)
./build.sh --backend

# Install Node.js dependencies
yarn install

# Build frontend for development
yarn start
```

### Running Locally

After building, the application binaries will be in `_output/`. The main executable varies by platform:

- **Windows**: `Prowlarr.exe`
- **Linux/macOS**: `Prowlarr` (executable)

Configuration files and data are stored in platform-specific locations as documented in the application.
