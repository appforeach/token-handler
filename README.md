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
   - Login with admin/admin
   - Create a new realm named "poc"
   - Create a new client:
     - Client ID: poc-api
     - Client Protocol: openid-connect
     - Access Type: confidential
     - Valid Redirect URIs: http://localhost:3000/*
     - Web Origins: http://localhost:3000
   - Create a new user:
     - Username: test
     - Email: test@example.com
     - Password: test123
     - Email Verified: true



First, log in to the Keycloak admin console at http://localhost:8080 with:
Username: admin
Password: admin
Once logged in, you'll see the Keycloak admin console. To create a new realm:
Look for the dropdown in the top-left corner that says "master" (this is the default realm)
Click on it and select "Create Realm"
In the "Realm name" field, enter: poc
Click "Create"
After creating the realm, you'll be automatically switched to the new "poc" realm. You can verify this by checking the dropdown in the top-left corner - it should now say "poc"
Next, we need to create a client. In the left menu:
Click on "Clients"
Click "Create client"
Fill in the following:
Client ID: poc-api
Client Protocol: openid-connect
Root URL: http://localhost:3000
Click "Next"
In the Capability config:
Enable "Client authentication"
Access Type: confidential
Click "Next"
In the Login settings:
Valid Redirect URIs: http://localhost:3000/*
Web Origins: http://localhost:3000
Click "Save"
After creating the client, you'll need to get the client secret:
Click on the "poc-api" client
Go to the "Credentials" tab
Copy the "Client secret" - you'll need this for the API configuration

3. Access the application:
   - Frontend: http://localhost:3000
   - API: http://localhost:5000/api
   - Keycloak: http://localhost:8080
	- 
4. Add keycloak host name to the hosts file

127.0.0.1 keycloak #this is needed for internal communication with keycloack within docker compose

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


