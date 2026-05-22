# SETUP-CHANGELOG.md

## What Changed from the Base Template

### App Identity
- **Renamed** from `CopilotBlazorTemplate` → `ITSupportDesk` via `scripts/init-app.sh`
- **Branded** as "IT Support Desk" — a help-desk application for submitting and tracking IT support tickets

### Domain Model (Core Project)
| Addition | Description |
|----------|-------------|
| `SupportTicket` entity | Title, Description, Priority, Status, Category, CreatedBy, AssignedTo, timestamps |
| `TicketComment` entity | Threaded comments on tickets with author and timestamp |
| `TicketStatus` enum | Open, InProgress, Resolved, Closed |
| `TicketPriority` enum | Low, Medium, High, Critical |
| `TicketCategory` enum | Hardware, Software, Network, Access, Other |
| `ITicketService` / `TicketService` | CRUD operations with role-based access control |
| `AppDbContext` update | DbSets, `OnModelCreating` with FK constraints, string enum conversion |
| `SeedData` update | 5 sample tickets + 1 comment seeded across admin and user accounts |
| EF Migration | `AddSupportTickets` migration for the new tables |

### UI Pages (Web Project)
| Page | Route | Description |
|------|-------|-------------|
| Landing (`Home.razor`) | `/` | Rebranded with 🎫 icon, "IT Support Desk" title, help-desk tagline |
| Dashboard (`Dashboard.razor`) | `/dashboard` | Ticket stats cards (Open/InProgress/Resolved/Closed), recent tickets table |
| Ticket List (`Tickets/Index.razor`) | `/tickets` | Filterable ticket table; admin sees all, user sees own |
| Create Ticket (`Tickets/Create.razor`) | `/tickets/create` | Form with title, description, category, priority; InteractiveServer |
| Ticket Detail (`Tickets/Detail.razor`) | `/tickets/{id}` | Full ticket view, comments, admin actions (assign/status change) |
| Admin Panel (`Admin.razor`) | `/admin` | Added ticket stats summary (total, unassigned, critical, high) |

### Layout & Sidebar
- Sidebar brand → "🎫 IT Support Desk"
- Added "My Tickets" and "New Ticket" navigation links for all authenticated users
- Admin link remains admin-only via `<AuthorizeView>`

### Theming (CSS)
- **Primary color**: Changed from dark grey (#171717) to IT-blue (#2563eb)
- **Background**: Soft slate (#f8fafc) instead of pure white
- Added **status badge colors**: Open (amber), In Progress (blue), Resolved (green), Closed (grey)
- Added **priority badge colors**: Low (grey), Medium (amber), High (orange), Critical (red)
- Added form input/select/textarea styles, secondary button, stats grid, comment thread styles
- Added ticket detail layout styles (meta, description, info grid)

### Tests
| Category | Count | Description |
|----------|-------|-------------|
| Unit tests (existing) | 5 | Unchanged — ApplicationUser + SeedData tests still pass |
| Unit tests (new) | 15 | SupportTicketTests (3), TicketCommentTests (2), TicketServiceTests (10) |
| E2E tests (existing) | 21 | Updated HomeTests for new landing text; all others unchanged |
| E2E tests (new) | 11 | Ticket list, create flow, detail view, admin actions, authorization |
| **Total** | **52** | All green |

### Demo & Screenshots Config
- Updated `docs/screenshots.config.json` with new pages: `/tickets`, `/tickets/create`
- Added `user` credentials for demo
- Updated demo storyboard: 15 steps covering user ticket lifecycle + admin management
- Demo video filename: `it-support-desk-demo.webm`

### What Was NOT Changed
- ASP.NET Identity setup (cookie auth, no registration)
- SQLite database with auto-migration at startup
- Seeded credentials (admin@template.local / user@template.local)
- Error/NotFound/Account pages
- EF Core + SQLite stack
- .NET 10 TFM
- Template's CSS variable system (extended, not replaced)
