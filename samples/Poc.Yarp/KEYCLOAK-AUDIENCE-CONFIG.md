# Keycloak Configuration for Audience-Based Security Pattern

## Goal
Configure Keycloak so that:
1. **BFF tokens** have ONLY `"aud": ["bff"]`
2. **API tokens** have ONLY `"aud": ["api"]`
3. **Token exchange** from BFF ? API is allowed via authorization policy
4. **Direct use** of BFF tokens against API is prevented

## Step-by-Step Configuration

### Step 1: Configure BFF Client (Restrict Audience)

1. **Keycloak Admin Console** ? **Clients** ? **bff**

2. **Remove the API audience mapper:**
   - Go to **Client scopes** tab ? **bff-dedicated** (or dedicated scope)
   - Look for **Mappers** list
   - Find and **DELETE** the mapper named `api-audience-mapper`
   - Keep ONLY the `audience-mapper` that adds `"bff"` audience

3. **Verify Protocol Mappers:**
   ```json
   {
     "name": "audience-mapper",
     "protocol": "openid-connect",
     "protocolMapper": "oidc-audience-mapper",
     "config": {
       "included.client.audience": "bff",
       "id.token.claim": "false",
       "access.token.claim": "true"
     }
   }
   ```

4. **Verify Client Scopes:**
   - **Default Client Scopes**: email, profile
   - **Optional Client Scopes**: Remove any scope that adds `api` audience

5. **Test Result:**
   ```bash
   # Get token
   curl -X POST http://localhost:8080/realms/poc/protocol/openid-connect/token \
     -H "Content-Type: application/x-www-form-urlencoded" \
     -d "grant_type=password" \
     -d "client_id=bff" \
     -d "client_secret=your-client-secret-here" \
     -d "username=admin" \
     -d "password=admin123"
   
   # Decode the access_token at https://jwt.io
   # Expected aud: ["bff"] only
   ```

### Step 2: Configure API Client (Keep API Audience)

1. **Keycloak Admin Console** ? **Clients** ? **api**

2. **Verify Protocol Mappers:**
   ```json
   {
     "name": "audience-mapper",
     "protocol": "openid-connect",
     "protocolMapper": "oidc-audience-mapper",
     "config": {
       "included.client.audience": "api",
       "id.token.claim": "false",
       "access.token.claim": "true"
     }
   }
   ```

3. **Enable Authorization Services:**
   - **Settings** tab ? **Capability config**
   - Enable **Authorization**
   - Click **Save**

### Step 3: Configure Token Exchange Authorization

This is the KEY step that allows BFF to exchange tokens for API audience without having it in the original token.

#### Option A: Using Keycloak Admin UI

1. **Keycloak Admin Console** ? **Clients** ? **api** ? **Authorization**

2. **Create a Resource for Token Exchange:**
   - **Authorization** ? **Resources** ? **Create resource**
   - **Name**: `Token Exchange Permission`
   - **Type**: `urn:keycloak:token-exchange`
   - **Resource Scopes**: Leave empty or add `token-exchange`
   - Click **Save**

3. **Create a Client Policy:**
   - **Authorization** ? **Policies** ? **Create policy** ? **Client**
   - **Name**: `bff-client-policy`
   - **Clients**: Select **bff**
   - **Logic**: Positive
   - Click **Save**

4. **Create a Permission:**
   - **Authorization** ? **Permissions** ? **Create permission** ? **Resource-based**
   - **Name**: `Token Exchange Permission`
   - **Resource**: Select `Token Exchange Permission`
   - **Policies**: Select `bff-client-policy`
   - **Decision Strategy**: Affirmative
   - Click **Save**

#### Option B: Using Keycloak Admin API

```bash
# Get admin token
ADMIN_TOKEN=$(curl -X POST "http://localhost:8080/realms/master/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "username=admin" \
  -d "password=admin" \
  -d "grant_type=password" \
  -d "client_id=admin-cli" | jq -r '.access_token')

# Get api client ID
API_CLIENT_ID=$(curl -X GET "http://localhost:8080/admin/realms/poc/clients" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq -r '.[] | select(.clientId=="api") | .id')

# Get bff client ID
BFF_CLIENT_ID=$(curl -X GET "http://localhost:8080/admin/realms/poc/clients" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq -r '.[] | select(.clientId=="bff") | .id')

# Create client policy
curl -X POST "http://localhost:8080/admin/realms/poc/clients/$API_CLIENT_ID/authz/resource-server/policy/client" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "bff-client-policy",
    "clients": ["'$BFF_CLIENT_ID'"],
    "logic": "POSITIVE"
  }'

# Create token exchange resource
RESOURCE_ID=$(curl -X POST "http://localhost:8080/admin/realms/poc/clients/$API_CLIENT_ID/authz/resource-server/resource" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Token Exchange Permission",
    "type": "urn:keycloak:token-exchange",
    "scopes": []
  }' | jq -r '.id')

# Get policy ID
POLICY_ID=$(curl -X GET "http://localhost:8080/admin/realms/poc/clients/$API_CLIENT_ID/authz/resource-server/policy" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq -r '.[] | select(.name=="bff-client-policy") | .id')

# Create permission
curl -X POST "http://localhost:8080/admin/realms/poc/clients/$API_CLIENT_ID/authz/resource-server/permission/resource" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Token Exchange Permission",
    "resources": ["'$RESOURCE_ID'"],
    "policies": ["'$POLICY_ID'"],
    "decisionStrategy": "AFFIRMATIVE"
  }'
```

### Step 4: Update Realm Configuration File

If using the `poc-realm.json` file for realm import/export, update the `api` client configuration:

```json
{
  "clientId": "api",
  "authorizationServicesEnabled": true,
  "authorizationSettings": {
    "policyEnforcementMode": "ENFORCING",
    "resources": [
      {
        "name": "Token Exchange Permission",
        "type": "urn:keycloak:token-exchange",
        "scopes": []
      }
    ],
    "policies": [
      {
        "name": "bff-client-policy",
        "type": "client",
        "logic": "POSITIVE",
        "clients": ["bff"]
      }
    ],
    "permissions": [
      {
        "name": "Token Exchange Permission",
        "type": "resource",
        "resources": ["Token Exchange Permission"],
        "policies": ["bff-client-policy"]
      }
    ]
  }
}
```

### Step 5: Verify Configuration

#### Test 1: BFF Token Has Restricted Audience
```http
POST http://localhost:8080/realms/poc/protocol/openid-connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=password
&client_id=bff
&client_secret=your-client-secret-here
&username=admin
&password=admin123
```

**Expected JWT payload:**
```json
{
  "aud": ["bff"],  // ? ONLY bff, not ["bff", "api"]
  "sub": "...",
  "azp": "bff"
}
```

#### Test 2: Direct API Call Fails
```bash
# Using BFF token from Test 1
curl -X GET http://localhost:5001/weatherforecast \
  -H "Authorization: Bearer <bff_token>"

# Expected: 401 Unauthorized
```

#### Test 3: Token Exchange Succeeds
```http
POST http://localhost:8080/realms/poc/protocol/openid-connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=urn:ietf:params:oauth:grant-type:token-exchange
&client_id=bff
&client_secret=your-client-secret-here
&subject_token=<bff_token>
&subject_token_type=urn:ietf:params:oauth:token-type:access_token
&requested_token_type=urn:ietf:params:oauth:token-type:access_token
&audience=api
```

**Expected JWT payload:**
```json
{
  "aud": ["api"],  // ? Changed from "bff" to "api"
  "sub": "...",    // ? Same user
  "azp": "bff"     // ? Original requesting client
}
```

#### Test 4: API Call with Exchanged Token Succeeds
```bash
# Using exchanged token from Test 3
curl -X GET http://localhost:5001/weatherforecast \
  -H "Authorization: Bearer <exchanged_token>"

# Expected: 200 OK with weather data
```

## Troubleshooting

### Problem: Token exchange returns 403 Forbidden
**Cause:** Authorization policy not configured correctly

**Solution:**
1. Verify **Authorization** is enabled on `api` client
2. Check that **bff-client-policy** exists and includes `bff` client
3. Verify **Token Exchange Permission** exists and uses the policy
4. Check Keycloak server logs: `docker logs keycloak`

### Problem: BFF token still has both audiences
**Cause:** Multiple audience mappers configured

**Solution:**
1. Go to **Clients** ? **bff** ? **Client scopes** ? **bff-dedicated**
2. Remove ALL mappers that add `api` audience
3. Check **Assigned default client scopes** for scopes that might add `api` audience
4. Test with a new token

### Problem: Token exchange changes the subject
**Cause:** Impersonation being applied unintentionally

**Solution:**
1. Remove `requested_subject` parameter from exchange request
2. Verify the `sub` claim is preserved from original token

### Problem: API still accepts BFF tokens
**Cause:** API not validating audience

**Solution:**
Check `Poc.Api\Program.cs`:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://localhost:8080/realms/poc";
        options.Audience = "api";  // ? Must be set
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,  // ? Must be true
            // ...
        };
    });
```

## Security Verification Checklist

- [ ] BFF tokens have ONLY `"aud": ["bff"]`
- [ ] API tokens have ONLY `"aud": ["api"]`
- [ ] Direct API call with BFF token returns 401
- [ ] Token exchange from BFF ? API succeeds
- [ ] API call with exchanged token succeeds
- [ ] Token exchange preserves `sub` claim
- [ ] Token exchange updates `jti` and `iat` claims
- [ ] Authorization policy is enforced in Keycloak

## References

- [Keycloak Token Exchange Documentation](https://www.keycloak.org/docs/latest/securing_apps/#_token-exchange)
- [Keycloak Authorization Services](https://www.keycloak.org/docs/latest/authorization_services/)
- [RFC 8693 - OAuth 2.0 Token Exchange](https://www.rfc-editor.org/rfc/rfc8693.html)
