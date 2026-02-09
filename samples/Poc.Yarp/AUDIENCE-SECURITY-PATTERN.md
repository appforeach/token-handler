# Audience-Based Token Security Pattern

## Your Security Goal
Make BFF tokens unusable directly against backend APIs, forcing token exchange for proper audience targeting.

## The Problem You Discovered

You noticed that token exchange from `bff` to `api` only works when the original token has:
```json
{
  "aud": ["bff", "api"],
  "sub": "add55713-a747-48f8-bb3f-78d5c852ea6f",
  ...
}
```

But you want BFF tokens to be restricted and NOT include backend API audiences.

## The Solution: Audience Restriction Pattern

### 1. Understanding JWT `aud` Claim in Token Exchange

According to RFC 8693 and Keycloak's implementation:

#### ? Token Exchange WITH Audience Present
- **Subject token** includes target audience: `"aud": ["bff", "api"]`
- **Exchange works** because the authorization server sees the requester already has access
- **Security issue**: BFF token can be used directly against the API

#### ? Token Exchange WITHOUT Target Audience
- **Subject token** only includes BFF audience: `"aud": ["bff"]`
- **Exchange fails** in Keycloak because the original token doesn't have `api` audience
- **Security benefit**: BFF token cannot be used directly against the API

### 2. The Correct Security Pattern

For your use case, you have TWO options:

#### Option A: Strict Audience Isolation (Recommended for Zero Trust)

**Configuration:**
```json
{
  "clientId": "bff",
  "protocolMappers": [
    {
      "name": "bff-audience-only",
      "protocol": "openid-connect",
      "protocolMapper": "oidc-audience-mapper",
      "config": {
        "included.client.audience": "bff",
        "id.token.claim": "false",
        "access.token.claim": "true"
      }
    }
    // DO NOT include api-audience-mapper here
  ]
}
```

**Token Exchange Configuration:**
You need to configure Keycloak's token exchange policies to allow `bff` to exchange tokens for `api` audience even without it being present in the original token.

In Keycloak Admin Console:
1. **Clients** ? `api` ? **Permissions** ? **token-exchange** ? Enable
2. **Add Policy**: Create "Allow BFF to Exchange" policy
3. **Add Permission**: Link the policy to allow `bff` client to request `api` audience

**The key**: Keycloak's authorization policy allows the exchange based on client permissions, NOT just token contents.

#### Option B: Optional Audience (Current Setup - Less Secure)

Your current setup includes `api` as an optional client scope, but the mapper adds it automatically. This defeats your security goal.

### 3. Implementing Option A (Strict Isolation)

#### Step 1: Update Keycloak Realm Configuration

Remove the `api-audience-mapper` from the `bff` client and make it only add its own audience:

```json
{
  "clientId": "bff",
  "protocolMappers": [
    {
      "name": "audience-mapper",
      "protocol": "openid-connect",
      "protocolMapper": "oidc-audience-mapper",
      "consentRequired": false,
      "config": {
        "included.client.audience": "bff",
        "id.token.claim": "false",
        "access.token.claim": "true"
      }
    }
    // Remove api-audience-mapper
  ]
}
```

#### Step 2: Configure Token Exchange Permissions

In Keycloak, you need to set up resource-based permissions for token exchange:

1. Go to **Clients** ? **api**
2. Enable **Authorization** if not already enabled
3. Go to **Authorization** ? **Permissions** ? **token-exchange**
4. Create a **Client Policy** that allows `bff` to exchange tokens
5. This permits the exchange DESPITE the lack of `api` audience in the subject token

#### Step 3: Update Your Token Exchange Request

The exchange request remains the same, but now it works based on authorization policy:

```http
POST {{Poc.KeyCloak_TokenEndpoint}}
Content-Type: application/x-www-form-urlencoded

grant_type=urn:ietf:params:oauth:grant-type:token-exchange
&client_id=bff
&client_secret=your-client-secret-here
&subject_token={{bffToken}}
&subject_token_type=urn:ietf:params:oauth:token-type:access_token
&requested_token_type=urn:ietf:params:oauth:token-type:access_token
&audience=api
```

**Result:** Exchanged token will have `"aud": ["api"]` even though the original token only had `"aud": ["bff"]`

### 4. Why This Works

Keycloak's token exchange permission system has two layers:

1. **Token-based validation**: Checks if the subject token has access to the requested audience
2. **Policy-based validation**: Checks if the requesting client is authorized to perform the exchange

Your configuration needs to rely on **policy-based validation** rather than token-based validation.

### 5. API Validation Logic

Your backend API should validate the audience claim:

```csharp
// In your API authentication configuration
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://localhost:8080/realms/poc";
        options.Audience = "api"; // CRITICAL: Reject tokens without 'api' audience
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = "api",
            // BFF tokens with only "bff" audience will be REJECTED
        };
    });
```

## Security Benefits

### ? With This Pattern:
- BFF tokens cannot be used directly against backend APIs (different audience)
- Attackers who compromise BFF tokens cannot access backend services
- Token exchange provides an audit trail (new `jti`, `iat`, etc.)
- You can apply additional authorization logic during exchange
- Each service validates its own audience requirement

### ? Without This Pattern (Current Setup):
- BFF tokens have both `bff` and `api` audiences
- Compromised BFF tokens can directly access backend APIs
- No token exchange needed, so no audit trail for service-to-service calls
- Single token used across trust boundaries

## Testing the Implementation

### Test 1: Verify BFF Token Audience Restriction
```http
### Get BFF Token
POST {{Poc.KeyCloak_TokenEndpoint}}
Content-Type: application/x-www-form-urlencoded

grant_type=password
&client_id=bff
&client_secret=your-client-secret-here
&username=admin
&password=admin123

# Expected: Token should have ONLY "aud": ["bff"]
```

### Test 2: Verify Direct API Access Fails
```http
### Try to access API directly with BFF token
GET {{Poc.Api_HostAddress}}/weatherforecast
Authorization: Bearer {{bffToken}}

# Expected: 401 Unauthorized (audience validation fails)
```

### Test 3: Verify Token Exchange Works
```http
### Exchange BFF token for API token
POST {{Poc.KeyCloak_TokenEndpoint}}
Content-Type: application/x-www-form-urlencoded

grant_type=urn:ietf:params:oauth:grant-type:token-exchange
&client_id=bff
&client_secret=your-client-secret-here
&subject_token={{bffToken}}
&subject_token_type=urn:ietf:params:oauth:token-type:access_token
&requested_token_type=urn:ietf:params:oauth:token-type:access_token
&audience=api

# Expected: Success, new token with "aud": ["api"]
```

### Test 4: Verify API Access with Exchanged Token
```http
### Access API with exchanged token
GET {{Poc.Api_HostAddress}}/weatherforecast
Authorization: Bearer {{apiToken}}

# Expected: 200 OK
```

## Important Considerations

### 1. Token Exchange Permissions Are Key
The success of this pattern depends on proper Keycloak authorization policies, NOT just the presence of audiences in tokens.

### 2. Client Secret Security
Since `bff` needs to perform token exchange, it must have its own credentials. Protect the BFF client secret.

### 3. Performance Impact
Token exchange adds an extra round-trip to Keycloak. Consider:
- Caching exchanged tokens (with shorter expiry)
- Using the same exchanged token for multiple backend calls
- Implementing token refresh strategies

### 4. Sub Claim Preservation
The `sub` claim is preserved during exchange (unless doing impersonation), maintaining user context.

## References

- [RFC 8693 - OAuth 2.0 Token Exchange](https://www.rfc-editor.org/rfc/rfc8693.html)
- [RFC 7519 - JWT Audience Claim](https://www.rfc-editor.org/rfc/rfc7519.html#section-4.1.3)
- [Keycloak Token Exchange](https://www.keycloak.org/docs/latest/securing_apps/#_token-exchange)
- [Zero Trust Architecture](https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-207.pdf)
