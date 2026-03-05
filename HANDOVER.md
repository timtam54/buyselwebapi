# BuySel Web API - Developer Handover Document

## 1. Project Overview

**BuySel** is a real estate marketplace platform where sellers list properties and buyers can search, favourite, message sellers, and make offers. This repository contains the backend Web API built with **ASP.NET Core 8 Minimal APIs**.

**Frontend**: Next.js app deployed at `https://buysel-webapp.azurewebsites.net`
**Backend API**: Deployed at `https://buysel.azurewebsites.net`

---

## 2. Tech Stack

| Component | Technology |
|-----------|-----------|
| Framework | .NET 8 (ASP.NET Core Minimal APIs) |
| Database | Azure SQL Server (Entity Framework Core 8) |
| Authentication | JWT Bearer tokens (validated, issued by NextAuth on frontend) + OAuth (Google/Microsoft/Facebook via NextAuth) |
| Hosting | Azure App Service (Linux, Zip Deploy) |
| File Storage | Azure Blob Storage (photos, documents, IDs) |
| Push Notifications | Web Push (WebPush library) + iOS APNS (dotAPNS library) |
| API Docs | Swagger / Swashbuckle (development only) |
| Geocoding | Google Maps Geocoding API (via IHttpClientFactory) |

### NuGet Packages

- `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.20
- `Microsoft.EntityFrameworkCore.SqlServer` 8.0.20
- `Microsoft.EntityFrameworkCore.Tools` 8.0.20
- `Swashbuckle.AspNetCore` 6.6.2
- `WebPush` 1.0.12
- `dotAPNS` 4.6.0

---

## 3. Project Structure

```
buyselwebapi/
  Program.cs                  # App entry point, middleware, DI, route mapping
  buyselwebapi.csproj         # Project file & dependencies
  appsettings.json            # App configuration
  data/
    dbcontext.cs              # EF Core DbContext with all DbSets
  model/
    user.cs                   # User, OAuthUserRequest
    property.cs               # Property, PropertyPic
    propertyphoto.cs          # PropertyPhoto
    badge.cs                  # Badge
    audit.cs                  # Audit, AudSummary
    Conversations.cs          # Conversation, ConversationCount, Message
    PushSubscription.cs       # PushSubscription (web + native)
    PropertyBuyerDoc.cs       # PropertyBuyerDoc
    userpropertyfavs.cs       # UserPropertyFav
    offer.cs                  # Offer
    OfferHistory.cs           # OfferHistory
    OfferCondition.cs         # OfferCondition
  endpoint/
    AuthHelper.cs             # Centralized auth: GetCurrentUser(), IsAdmin()
    userEP.cs                 # User CRUD + OAuth
    propertyEP.cs             # Property CRUD + search + geocoding
    propertyphotoEP.cs        # Property photo/document management
    badgeEP.cs                # Badge listing
    auditEP.cs                # Audit log viewing + management
    conversationEP.cs         # Buyer-seller conversations
    messageEP.cs              # Chat messages within conversations
    pushsubscriptionEP.cs     # Push notification subscriptions
    propertybuyerdocEP.cs     # Buyer document requests
    userpropertyfavEP.cs      # Property favourites
    offerEP.cs                # Offers + counter-offers
    offerConditionEP.cs       # Offer conditions/contingencies
    offerHistoryEP.cs         # Offer audit trail
  Properties/
    launchSettings.json       # Dev server ports (5005 / 7188)
    PublishProfiles/           # Azure deploy profile
```

---

## 4. Running Locally

### Prerequisites
- .NET 8 SDK
- Access to the Azure SQL database (connection string is in `Program.cs`)

### Start the API
```bash
dotnet run
```
The API will start on `http://localhost:5005` with Swagger UI at `/swagger` (development only).

### HTTPS
```bash
dotnet run --launch-profile https
```
Runs on `https://localhost:7188`.

---

## 5. Database

### Connection
- **Server**: `buyselserver.database.windows.net`
- **Database**: `buysel`
- **ORM**: Entity Framework Core (code-first style with `dbcontext.cs`)

### DbContext Entity Sets

| DbSet | Model | Table |
|-------|-------|-------|
| `user` | User | Users |
| `property` | Property | Properties |
| `propertyphoto` | PropertyPhoto | Property photos |
| `badge` | Badge | Badges |
| `audit` | Audit | Audit log |
| `audsummary` | AudSummary | Audit summary (read-only) |
| `conversation` | Conversation | Buyer-seller conversations |
| `conversationcount` | ConversationCount | Unread counts (read-only) |
| `message` | Message | Chat messages |
| `pushsubscriptions` | PushSubscription | Push notification registrations |
| `propertybuyerdoc` | PropertyBuyerDoc | Document requests |
| `userpropertyfav` | UserPropertyFav | Favourited properties |
| `offer` | Offer | Purchase offers |
| `offercondition` | OfferCondition | Offer contingencies |
| `offerhistory` | OfferHistory | Offer change log |

### Stored Procedures / Raw SQL
- `clearaudit` - Clears audit log entries
- `companyauditsummary` - Returns weekly audit summary (page, count, week-end date)
- `unreadConv` - Returns unread conversation counts per user

---

## 6. Authentication & Authorization

### JWT Configuration
- **Secret Key**: `BuySellCharterTowers`
- **Issuer**: `BuySell`
- **Audience**: `CharterTowers`
- Tokens are issued by **NextAuth** on the frontend and validated by the backend
- `RequireHttpsMetadata` is enforced in production, relaxed in development

### Auth Helper (`AuthHelper.cs`)
Centralized authentication utility used across all endpoints:
- `GetCurrentUserEmail(ClaimsPrincipal)` - Extracts email from JWT claims (checks `ClaimTypes.Email`, `"email"`, `ClaimTypes.NameIdentifier`, `"sub"`)
- `GetCurrentUser(ClaimsPrincipal, dbcontext)` - Looks up the User record by email
- `IsAdmin(ClaimsPrincipal, dbcontext)` - Returns true if user has `admin == true`

### Middleware Order (in Program.cs)
1. Security Headers
2. HTTPS Redirection
3. CORS
4. Rate Limiter
5. Authentication
6. Authorization

### Global Authorization
All endpoints require authorization by default via `app.MapGroup("").RequireAuthorization()`. Specific endpoints opt out with `.AllowAnonymous()`:
- `POST /api/users/oauth` (OAuth login/register)
- `POST /api/user/` (registration)
- `POST /api/audit/` (analytics tracking)
- `GET /api/property/` (browse published properties)
- `GET /api/property/{id}` (view single property)
- `GET /api/property/sellerusername/{id}` (view seller listings)
- `GET /api/property/postsubbedbath/*` (property search)

### Ownership / Access Control
Every protected endpoint enforces ownership checks:
- **Users**: Can only view/update their own profile. Cannot self-promote to admin.
- **Properties**: Only the seller (or admin) can update/delete. `sellerid` is forced to current user on create.
- **Conversations**: Only participants (buyer or seller) can view/send messages.
- **Messages**: Only conversation participants can read/send. `sender_id` forced to current user on create.
- **Offers**: Only buyer or property seller can view/modify. `buyer_id` forced to current user on create.
- **Offer Conditions/History**: Only offer participants (buyer or property seller).
- **Favourites**: Users can only manage their own. `user_id` forced to current user on create.
- **Buyer Doc Requests**: Buyer or property seller can view. `buyerid` forced to current user on create.
- **Photos**: Only the property seller can add/update/delete.
- **Audit, User listing, Property /all/**: Admin only.

### OAuth Support
The `/api/users/oauth` endpoint accepts Google/Microsoft/Facebook OAuth data (`OAuthUserRequest` model) and creates or updates users. This is the primary authentication flow.

---

## 7. Security Features

### Rate Limiting
- **Auth endpoints** (`/api/users/oauth`, `POST /api/user/`): 10 requests/minute per client
- **General endpoints**: 60 requests/minute per client
- Returns HTTP 429 when exceeded

### Security Headers
Applied to all responses via middleware:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`

### HTTPS
- `UseHttpsRedirection()` enabled
- `RequireHttpsMetadata` enforced in production

### Input Validation
All POST/PUT endpoints validate required fields and return `400 Bad Request` with descriptive error messages for invalid input.

### SQL Injection Prevention
- All raw SQL uses `FromSqlInterpolated()` (parameterized queries)
- No `FromSqlRaw()` with string concatenation

### Structured Logging
- All logging uses `ILogger` (no `Console.WriteLine`)
- Exception details are not leaked to API responses

### Swagger
- Only enabled in development (`app.Environment.IsDevelopment()`)
- Not exposed in production

---

## 8. CORS Policy

Allowed origins:
- `http://localhost:3000` (local Next.js dev)
- `https://buysel-webapp.azurewebsites.net` (production frontend)

Allows any header, any method, and credentials.

---

## 9. API Endpoints Reference

### User (`/api/user`)
| Method | Route | Auth | Access | Description |
|--------|-------|------|--------|-------------|
| POST | `/api/users/oauth` | Anonymous | Public | OAuth login/register |
| POST | `/api/user/` | Anonymous | Public | Register new user (admin=false enforced) |
| GET | `/api/user/{id}` | Required | Own profile or admin | Get user by ID |
| GET | `/api/user/email/{id}` | Required | Own email or admin | Get user by email |
| GET | `/api/user/` | Required | Admin only | Get all users (paginated) |
| GET | `/api/user/sellers/` | Required | Authenticated | Get users who are sellers |
| PUT | `/api/user/` | Required | Own profile or admin | Update user (no self-promote to admin) |
| DELETE | `/api/user/{id}` | Required | Admin only | Delete user |

### Property (`/api/property`)
| Method | Route | Auth | Access | Description |
|--------|-------|------|--------|-------------|
| GET | `/api/property/{id}` | Anonymous | Public | Get by ID |
| GET | `/api/property/` | Anonymous | Public | Get all published properties (with thumbnail) |
| GET | `/api/property/sellerusername/{id}` | Anonymous | Public | Get by seller email |
| GET | `/api/property/postsubbedbath/{postcode}/{beds}/{baths}` | Anonymous | Public | Search by location/beds/baths |
| GET | `/api/property/postsubbedbath/{postcode}/{beds}/{baths}/{user}` | Anonymous | Public | Search (with audit) |
| GET | `/api/property/seller/{id}` | Required | Own listings or admin | Get by seller ID |
| GET | `/api/property/audited/{id}` | Required | Admin only | Get all with audit logging |
| GET | `/api/property/all/` | Required | Admin only | Get all properties (any status) |
| GET | `/api/property/favs/{id}` | Required | Own favourites or admin | Get user's favourited properties |
| POST | `/api/property/` | Required | Authenticated | Create property (sellerid forced to current user) |
| PUT | `/api/property/` | Required | Property seller or admin | Update property |
| DELETE | `/api/property/{id}` | Required | Property seller or admin | Delete property |

### Property Photos (`/api/propertyphoto`)
| Method | Route | Auth | Access | Description |
|--------|-------|------|--------|-------------|
| GET | `/api/propertyphoto/{id}` | Required | Authenticated | Get photos for property |
| GET | `/api/propertyphoto/docs/{id}` | Required | Authenticated | Get documents for property |
| POST | `/api/propertyphoto/` | Required | Property seller or admin | Add photo/document |
| PUT | `/api/propertyphoto/` | Required | Property seller or admin | Update photo |
| DELETE | `/api/propertyphoto/{id}` | Required | Property seller or admin | Delete photo |

### Conversations (`/api/conversation`)
| Method | Route | Auth | Access | Description |
|--------|-------|------|--------|-------------|
| GET | `/api/conversation/{id}` | Required | Participants only | Get by ID |
| GET | `/api/conversation/user/{id}` | Required | Own or admin | Get all for user |
| GET | `/api/conversation/property/{propertyId}` | Required | Property seller or admin | Get all for property |
| GET | `/api/conversation/buyer/{buyerId}` | Required | Own or admin | Get by buyer |
| GET | `/api/conversation/seller/{sellerId}` | Required | Own or admin | Get by seller |
| GET | `/api/conversation/unread/{userId}` | Required | Own or admin | Unread counts |
| POST | `/api/conversation/` | Required | Authenticated | Start conversation (buyer_id forced) |
| PUT | `/api/conversation/` | Required | Participants only | Update |
| DELETE | `/api/conversation/{id}` | Required | Participants only | Delete |

### Messages (`/api/message`)
| Method | Route | Auth | Access | Description |
|--------|-------|------|--------|-------------|
| GET | `/api/message/{id}` | Required | Conversation participant | Get by ID |
| GET | `/api/message/conversation/{conversationId}` | Required | Conversation participant | Get all in conversation |
| GET | `/api/message/unread/{userId}` | Required | Own or admin | Unread messages for user |
| GET | `/api/message/unread/{userId}/{conversationid}` | Required | Own or admin | Unread in specific conversation |
| POST | `/api/message/` | Required | Conversation participant | Send message (sender_id forced) |
| PUT | `/api/message/` | Required | Conversation participant | Update message |
| PUT | `/api/message/markread/{id}` | Required | Conversation participant | Mark one as read |
| PUT | `/api/message/markread/{id}/{conversationid}` | Required | Conversation participant | Mark all read in conversation |
| DELETE | `/api/message/{id}` | Required | Conversation participant | Delete |

### Offers (`/api/offer`)
| Method | Route | Auth | Access | Description |
|--------|-------|------|--------|-------------|
| GET | `/api/offer/{id}` | Required | Buyer or property seller | Get by ID |
| GET | `/api/offer/property/{propertyId}` | Required | Property seller or admin | Get all for property |
| GET | `/api/offer/seller/{id}` | Required | Own or admin | Get all for seller |
| GET | `/api/offer/buyer/{buyerId}` | Required | Own or admin | Get all for buyer |
| POST | `/api/offer/` | Required | Authenticated | Create offer (buyer_id forced) |
| PUT | `/api/offer/` | Required | Buyer or property seller | Update offer (accept/reject/withdraw) |
| POST | `/api/offer/{id}/counter` | Required | Buyer or property seller | Counter-offer |

### Offer Conditions (`/api/offercondition`)
| Method | Route | Auth | Access | Description |
|--------|-------|------|--------|-------------|
| GET | `/api/offercondition/{offer_id}` | Required | Offer participant | Get conditions for offer |
| POST | `/api/offercondition/` | Required | Offer participant | Add condition |
| PUT | `/api/offercondition/` | Required | Offer participant | Update condition |
| PUT | `/api/offercondition/{id}/satisfy` | Required | Offer participant | Mark satisfied |
| DELETE | `/api/offercondition/{id}` | Required | Offer participant | Delete |

### Offer History (`/api/offerhistory`)
| Method | Route | Auth | Access | Description |
|--------|-------|------|--------|-------------|
| GET | `/api/offerhistory/{id}` | Required | Offer participant | Get by ID |
| GET | `/api/offerhistory/offer/{offerId}` | Required | Offer participant | Get history for offer |
| POST | `/api/offerhistory/` | Required | Offer participant | Add history entry (actor_id forced) |
| PUT | `/api/offerhistory/` | Required | Offer participant | Update |
| DELETE | `/api/offerhistory/{id}` | Required | Admin only | Delete |

### Buyer Document Requests (`/api/propertybuyerdoc`)
| Method | Route | Auth | Access | Description |
|--------|-------|------|--------|-------------|
| GET | `/api/propertybuyerdoc/{id}` | Required | Buyer or property seller | Get doc requests for property |
| GET | `/api/propertybuyerdoc/` | Required | Admin only | Outstanding doc requests |
| GET | `/api/propertybuyerdoc/all/` | Required | Admin only | All doc requests |
| POST | `/api/propertybuyerdoc/` | Required | Authenticated | Create doc request (buyerid forced) |
| PUT | `/api/propertybuyerdoc/` | Required | Buyer or property seller | Update doc request |
| DELETE | `/api/propertybuyerdoc/{id}` | Required | Buyer or admin | Delete doc request |

### Push Subscriptions (`/api/push`)
| Method | Route | Auth | Access | Description |
|--------|-------|------|--------|-------------|
| POST | `/api/push/subscribe` | Required | Authenticated | Web push subscribe |
| POST | `/api/push/unsubscribe` | Required | Authenticated | Web push unsubscribe |
| POST | `/api/push/push_subscription` | Required | Authenticated | Web push subscribe (alt format) |
| GET | `/api/push/push_subscription/email/{email}` | Required | Authenticated | Get web-push subscriptions |
| POST | `/api/push/subscribe-native` | Required | Authenticated | Native (iOS/Android) subscribe |
| POST | `/api/push/unsubscribe-native` | Required | Authenticated | Native unsubscribe |
| GET | `/api/push/subscriptions/{email}` | Required | Authenticated | Get subscriptions by email |
| DELETE | `/api/push/push_subscription/{id}` | Required | Authenticated | Delete subscription |

### Audit (`/api/audit`)
| Method | Route | Auth | Access | Description |
|--------|-------|------|--------|-------------|
| GET | `/api/audit/` | Required | Admin only | Audit records (paginated) |
| GET | `/api/audit/{id}` | Required | Admin only | Audit for property |
| GET | `/api/audit/summary/` | Required | Admin only | Weekly summary |
| DELETE | `/api/audit/clearaudit` | Required | Admin only | Clear audit log |
| POST | `/api/audit/` | Anonymous | Public | Create audit entry (analytics) |
| DELETE | `/api/audit/{id}` | Required | Admin only | Delete entry |

### Badge (`/api/badge`)
| Method | Route | Auth | Access | Description |
|--------|-------|------|--------|-------------|
| GET | `/api/badge/` | Required | Authenticated | List all badges |

### Favourites (`/api/userpropertyfav`)
| Method | Route | Auth | Access | Description |
|--------|-------|------|--------|-------------|
| GET | `/api/userpropertyfav/{id}` | Required | Own or admin | Get user favourites |
| POST | `/api/userpropertyfav/` | Required | Authenticated | Add favourite (user_id forced) |
| DELETE | `/api/userpropertyfav/{id}` | Required | Own or admin | Remove favourite |

---

## 10. Key Business Logic

### Property Lifecycle
1. Seller creates property (status defaults to draft/pending)
2. Admin reviews and sets status to `published` or `rejected` (with reason)
3. Published properties appear in buyer search results
4. Buyers can search by postcode/suburb, beds, baths

### Offer Workflow
1. Buyer creates offer on a property (status: `pending`)
2. Seller can accept, reject, or counter-offer
3. Counter-offers create a new offer linked via `parent_offer_id` with incremented `version`
4. Original offer status changes to `countered`
5. Conditions (finance, inspection, etc.) tracked separately with satisfaction deadlines
6. Full history recorded in `offerhistory`

### Messaging System
- Conversations are tied to a property, buyer, and seller
- Messages track read/unread status via `read_at` timestamp
- Bulk mark-as-read supported per conversation
- File attachments via Azure Blob URLs (`bloburl` field)

### Audit Trail
- Static `Audit` method used throughout endpoints to log actions
- Tracks: page, action, username (email), IP address, property ID
- Summary report aggregated weekly

### Push Notifications
- **Web Push**: Standard Web Push protocol (VAPID keys, p256dh, auth)
- **iOS**: APNS via dotAPNS library (P8 key authentication)
- **Android**: Device token based
- Subscriptions deduplicated by endpoint (web) or device token (native)

### Timestamps
All timestamps use `DateTime.UtcNow`.

### Geocoding
When a property is loaded without lat/lon coordinates, the API automatically geocodes the address using the Google Maps Geocoding API via `IHttpClientFactory`.

---

## 11. Deployment

### Azure App Service
- **Resource Group**: PlantAllocation
- **App Name**: buysel
- **URL**: https://buysel.azurewebsites.net
- **Runtime**: Linux x64
- **Deploy Method**: Zip Deploy from Visual Studio

### Publishing from Visual Studio
1. Right-click project > Publish
2. Select the "buysel - Zip Deploy" profile
3. Publish

### Publishing from CLI
```bash
dotnet publish -c Release -r linux-x64
```
Then deploy the output to Azure.

---

## 12. External Service Dependencies

| Service | Purpose | Notes |
|---------|---------|-------|
| Azure SQL Server | Database | `buyselserver.database.windows.net` |
| Azure Blob Storage | File/photo storage | URLs stored in DB fields |
| Azure App Service | API hosting | Linux x64 |
| Google Maps Geocoding API | Address to lat/lon | Key in `propertyEP.cs` |
| Apple APNS | iOS push notifications | P8 key in `pushsubscriptionEP.cs` |

---

## 13. Known Considerations

1. **Connection string is hardcoded** in `Program.cs` rather than in `appsettings.json` - consider moving to config/secrets for production
2. **JWT secret is hardcoded** (`BuySellCharterTowers`) - consider using environment variables or Azure Key Vault
3. **Google Maps API key** is hardcoded in `propertyEP.cs`
4. **APNS credentials** (P8 key, KeyId, TeamId) are hardcoded in `pushsubscriptionEP.cs`
5. **No EF migrations** tracked in the repo - database schema is managed externally
6. **CORS** is restricted to localhost:3000 and the Azure frontend URL

---

## 14. Quick Reference - Model Fields

### User
`id`, `email`, `firstname`, `lastname`, `middlename`, `mobile`, `address`, `dateofbirth`, `residencystatus`, `maritalstatus`, `powerofattorney`, `idtype`, `idbloburl`, `idverified`, `photoverified`, `photoazurebloburl`, `ratesnotice`, `ratesnoticeverified`, `titlesearch`, `titlesearchverified`, `termsconditions`, `privacypolicy`, `admin`, `dte`

### Property
`id`, `title`, `address`, `sellerid`, `price`, `lat`, `lon`, `typeofprop`, `suburb`, `postcode`, `state`, `country`, `beds`, `baths`, `carspaces`, `landsize`, `buildyear`, `titlesrchcouncilrateazureblob`, `titlesrchcouncilrateverified`, `titlesrchcouncilratepublic`, `buildinginspazureblob`, `buildinginspverified`, `buildinginsppublic`, `pestinspazureblob`, `pestinspverified`, `pestinsppublic`, `status`, `rejectedreason`, `contractsale`, `poolcert`, `smokealarm`, `dte`

### Offer
`id`, `property_id`, `buyer_id`, `status`, `offer_amount`, `deposit_amount`, `settlement_days`, `finance_days`, `inspection_days`, `conditions_json`, `expires_at`, `created_at`, `updated_at`, `parent_offer_id`, `version`
