# Sample Application

This is a proof of concept application that demonstrates the integration of:
- ASP.NET Core Web API with Keycloak authentication
- Keycloak for identity and access management
- YARP reverse proxy
- React minimalistic frontend
- Docker Compose setup

## Usage in YAPP projects

In Program.CS of your YAPP project, you can add the token handler as follows:

```csharp
  builder.Services.AddTokenHandler(options =>
        {
            options.Authority = builder.Configuration.GetValue<string>("Keycloak:Authority");
            options.ClientId = builder.Configuration.GetValue<string>("Keycloak:ClientId");
            options.ClientSecret = builder.Configuration.GetValue<string>("Keycloak:ClientSecret");
            options.Realm = builder.Configuration.GetValue<string>("Keycloak:Realm");
        });
```

Where
- Authority is a URL to your Keycloak instance (for instance http://localhost:8080/realms/poc), 
- ClientId is the ID of your Keycloak client
- ClientSecret is the secret of your Keycloak client
- Realm is the name of your Keycloak realm.


## Prerequisites

- Docker and Docker Compose
- .NET 9.0 SDK
- Node.js 16

## Setup Instructions (TODO: clean up this section)

1. Start the application:
```bash
docker-compose build
docker-compose up -d
```

2. Access Keycloak at http://localhost:8080
   - Realm "poc" is already created with a client and a user for testing purposes. See the file `.keycloak/realms/poc-realm.json`.
   - If you want to set it up manually, follow these steps:
   	- Create a new realm named "poc"
   	- Create a new client within this realm:
    	 - Client ID: poc-api
    	 - Client Protocol: openid-connect
    	 - Valid Redirect URIs: http://localhost:3000/*
    	 - Web Origins: http://localhost:3000
   	- Create a new user:
    	 - Username: test
    	 - Email: test@example.com
    	 - Password: test123
    	 - Password not temporary
    	 - Email Verified: true
    	- Get the client secret - needed for the API configuration
      - poc-api client
      - Credentials tab
      - Copy the "Client secret"

3. Access the application:
   - Frontend: http://localhost:3000
   - API: http://localhost:5000/api
   - Keycloak: http://localhost:8080
4. Add keycloak host name to the hosts file

127.0.0.1 keycloak #this is needed for internal communication with keycloak within docker compose


5. Shut down the application:
```bash
docker-compose down
docker-compose down -v (to remove volumes)
```

## Development

### Backend
```bash
cd samples\Poc.Api
dotnet run
```
### Yarp
```bash
cd samples\Poc.Yarp
dotnet run
```

### Frontend
```bash
cd samples\poc-frontend
npm install
npm run dev
```

## Architecture

- Frontend (React + TypeScript) runs on port 3000
- YARP reverse proxy runs on port 5198
- Backend API runs on port 8080 (internal) 
- Keycloak runs on port 8080
- PostgreSQL runs on port 5432 (internal) 


## Sequence Diagram of the Flow

```mermaid

sequenceDiagram
    participant Browser as Frontend #40;React#41;
    participant YARP as BFF/YARP Proxy
    participant TokenHandler as Token Handler Middleware
    participant Cache as Hybrid Cache
    participant Keycloak as Keycloak #40;IdP#41;
    participant API as Backend API

    Note over Browser,API: Initial Authentication Flow
    
    Browser->>YARP: 1. Access protected resource
    YARP->>TokenHandler: 2. Check authentication
    TokenHandler->>Browser: 3. Redirect to /Account/Login
    Browser->>YARP: 4. GET /Account/Login
    YARP->>Keycloak: 5. OIDC Authorization Request
    Keycloak->>Browser: 6. Login page
    Browser->>Keycloak: 7. Submit credentials
    Keycloak->>YARP: 8. Authorization code #40;callback#41;
    
    Note over YARP,Keycloak: Token Exchange
    YARP->>Keycloak: 9. Exchange code for tokens<br/>#40;OnAuthorizationCodeReceived#41;
    Keycloak->>YARP: 10. Access + Refresh tokens
    
    Note over YARP,Cache: Token Storage
    YARP->>Cache: 11. Store tokens with session-id<br/>#40;OnTokenValidated#41;
    YARP->>Browser: 12. Set session-id cookie #40;HttpOnly, Secure#41;
    YARP->>Browser: 13. Redirect to original URL

    Note over Browser,API: Subsequent API Calls

    Browser->>YARP: 14. API request with session-id cookie
    YARP->>TokenHandler: 15. AuthenticationHeaderSubstitutionMiddleware
    TokenHandler->>Cache: 16. Retrieve tokens by session-id
    Cache->>TokenHandler: 17. Return OAuthTokenResponse
    TokenHandler->>TokenHandler: 18. Add Authorization: Bearer {#35;access_token{#35;}
    TokenHandler->>API: 19. Proxied request with Bearer token
    API->>API: 20. Validate JWT token
    API->>TokenHandler: 21. API response
    TokenHandler->>Browser: 22. Response #40;without tokens#41;

    Note over Browser,API: Token Refresh #40;if needed#41;
    
    Browser->>YARP: 23. API request #40;expired token#41;
    TokenHandler->>Cache: 24. Get tokens
    TokenHandler->>Keycloak: 25. Refresh token request
    Keycloak->>TokenHandler: 26. New access token
    TokenHandler->>Cache: 27. Update cached tokens
    TokenHandler->>API: 28. Retry with new token

```