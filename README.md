# FastBiteGroupMCA

![.NET 8](https://img.shields.io/badge/.NET%208-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-DC382D?style=for-the-badge&logo=redis&logoColor=white)
![SignalR](https://img.shields.io/badge/SignalR-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Azure](https://img.shields.io/badge/Azure-0078D4?style=for-the-badge&logo=microsoftazure&logoColor=white)
![AWS](https://img.shields.io/badge/AWS-232F3E?style=for-the-badge&logo=amazonwebservices&logoColor=white)
![LiveKit](https://img.shields.io/badge/LiveKit-3183F5?style=for-the-badge&logo=webrtc&logoColor=white)
![MongoDB](https://img.shields.io/badge/MongoDB-47A248?style=for-the-badge&logo=mongodb&logoColor=white)
![Firebase](https://img.shields.io/badge/Firebase-FFCA28?style=for-the-badge&logo=firebase&logoColor=white)
![SendGrid](https://img.shields.io/badge/SendGrid-512BD4?style=for-the-badge&logo=twilio&logoColor=white)

**FastBiteGroupMCA** is a high-performance, scalable **Social Collaboration Platform** designed to facilitate real-time communication and community management. Built with **.NET 8** and **Clean Architecture**, it demonstrates enterprise-grade patterns including real-time synchronization, background processing, and distributed infrastructure.

---

## 🚀 Key Features

### 💬 Real-Time Communication

- **Instant Messaging**: WebSocket-based chat with real-time delivery status using **SignalR**.
- **Video & Audio Calls**: High-quality video conferencing integrated via **LiveKit**.
- **Presence Tracking**: Real-time user online/offline status monitoring using **Redis**.

### 👥 Community & Group Management

- **Advanced Group Roles**: Granular permission system (Admin, Moderator, Member).
- **Invitations System**: Secure group invitation handling.
- **Content Moderation**: Automated and manual tools to maintain community standards.

### 📝 Social Engagement

- **Interactive Feed**: Create rich-text posts with JSON/HTML storage support.
- **Nested Comments**: Reddit-style recursive comment threads.
- **Reactions & Polls**: Engage users with post reactions and voting polls.
- **Push Notifications**: Real-time alerts for likes, comments, and mentions.

---

## 🏗 System Architecture

The project follows strict **Clean Architecture** principles to ensure maintainability and testability:

- **Domain Layer**: Core business logic and entities (POCOs), independent of external frameworks.
- **Application Layer**: Use cases, DTOs, interfaces, and validation rules (MediatR/Services pattern).
- **Infrastructure Layer**: Implementation of external concerns (SignalR Hubs, Hangfire Jobs, LiveKit Service).
- **Persistence Layer**: Database access using **Entity Framework Core** and Repositories.

### 🗄️ Hybrid Database Strategy

This project demonstrates a sophisticated **Polyglot Persistence** approach:

- **SQL Server (Relational)**: Handles structured business data requiring strict consistency and complex relationships (Users, Groups, Permissions).
- **MongoDB (NoSQL)**: Utilized for high-volume, write-intensive data streams such as **Audit Logs**, **Notifications**, and **ChatMessage History**. This offloads pressure from the primary SQL DB and allows for flexible schema evolution.

### Infrastructure Components

- **Identity Server**: JWT-based authentication with Refresh Token rotation.
- **Background Jobs**: **Hangfire** for offloading heavy tasks (e.g., sending emails, processing content).
- **Caching**: **Redis** for distributed caching and real-time Pub/Sub.

---

## 🛠 Technology Stack

| Component            | Technology                                                                                         |
| -------------------- | -------------------------------------------------------------------------------------------------- |
| **Core Framework**   | .NET 8 (C#)                                                                                        |
| **Database**         | **SQL Server 2019** (Relational), **MongoDB** (NoSQL for High Volume)                              |
| **Cloud & Storage**  | **AWS S3** / **Azure Blob Storage** / **Firebase Storage** (Polymorphic), **Azure Content Safety** |
| **Real-time**        | **Azure SignalR** (Managed Service) / ASP.NET SignalR                                              |
| **Video/Audio**      | **LiveKit** (WebRTC SFU)                                                                           |
| **Email**            | **SendGrid** (Transactional Emails)                                                                |
| **Background Jobs**  | **Hangfire** (w/ Redis storage)                                                                    |
| **Logging**          | Serilog (Sinks: Console, File, Seq)                                                                |
| **Containerization** | Docker, Docker Compose                                                                             |
| **Object Mapping**   | AutoMapper                                                                                         |
| **Validation**       | FluentValidation                                                                                   |

---

## ⚙️ Getting Started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (optional, for local dev)

### Quick Start (Docker)

The easiest way to run the entire stack is via Docker Compose.

1. **Clone the repository**

   ```bash
   git clone https://github.com/LanHoangPh/FastBiteGroup_DATN_Public.git
   cd FastBiteGroupMCA
   ```

2. **Start Services**

   ```bash
   docker-compose up -d --build
   ```

   This will spin up:

   - `fastbite-api`: The backend API on port `8080`.
   - `sqlserver`: Main database.
   - `redis`: Caching layer.
   - `mongodb`: NoSQL store.

3. **Access the Application**
   - API Swagger UI: `http://localhost:8080/swagger`
   - Hangfire Dashboard: `http://localhost:8080/hangfire`

### Manual Setup (Local Development)

1. Update `appsettings.Development.json` with your local connection strings.
2. Run database migrations:
   ```bash
   dotnet ef database update --project FastBiteGroupMCA.Persistentce --startup-project FastBiteGroupMCA.API
   ```
3. Start the API:
   ```bash
   dotnet run --project FastBiteGroupMCA.API
   ```

---

### Testing Strategy

The solution includes a comprehensive test suite in `TestFastBiteGroupMCA`:

- **Unit Tests**: Validating individual business logic components.
- **Integration Tests**: Ensuring seamless interaction between different modules and the database.

---

## 🏗 Project Structure

```bash
FastBiteGroupMCA
├── FastBiteGroupMCA.API           # Presentation Layer (Controllers, Middleware)
├── FastBiteGroupMCA.Application   # Business Logic (DTOs, Interfaces, Services)
├── FastBiteGroupMCA.Domain        # Enterprise Entities & Event Definitions
├── FastBiteGroupMCA.Infrastructure # External Integrations (SQL, Mongo, Redis, Email)
├── FastBiteGroupMCA.Persistence   # Data Access (EF Core, Repositories)
└── TestFastBiteGroupMCA           # Unit & Integration Tests
```

---

## 👤 Author

Developed by **[LanHoangPh]**.
A showcase of modern backend engineering, distributed systems, and clean code practices.
