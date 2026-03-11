# ?? Quick Testing Checklist

## ? Configuration Complete

The following changes have been successfully applied:

1. ? `api-audience` moved from optional ? default scopes (BFF client)
2. ? `email` and `profile` client scopes added to realm with proper mappers
3. ? Test file updated with correct token exchange examples
4. ? Keycloak restarted to apply changes

## ?? Test Now (After Keycloak Starts)

Wait for Keycloak to be healthy (~30-60 seconds), then test:

### Test 1: Get BFF Token ?
Open `samples/Poc.Yarp/Test.Yarp.http` ? Run **Step 1**

Expected response:
```json
{
  "access_token": "eyJ...",
  "token_type": "Bearer",
  "expires_in": 600,
  "refresh_token": "eyJ...",
  "scope": "profile email"
}
```

Decode the `access_token` at https://jwt.io:
```json
{
  "aud": "bff",           // ? Only BFF audience
  "sub": "...",           // ? User ID present
  "email": "admin@...",   // ? Email present
  "name": "Admin User",   // ? Name present
  "scope": "profile email"
}
```

### Test 2: Exchange BFF ? API ?
Copy the `access_token` from Test 1 and paste it in `@adminAccessToken`

Run **Step 3** (no explicit scope needed!)

Expected response:
```json
{
  "access_token": "eyJ...",
  "issued_token_type": "urn:ietf:params:oauth:token-type:access_token",
  "token_type": "Bearer",
  "expires_in": 600
}
```

Decode the new `access_token`:
```json
{
  "aud": "api",           // ? Changed to API audience
  "sub": "...",           // ? User ID preserved
  "email": "admin@...",   // ? Email preserved
  "name": "Admin User",   // ? Name preserved
  "scope": "profile email"
}
```

### Test 3: Use API Token ?
Copy the exchanged `access_token` and paste it in `@apiAccessToken`

Run **Step 4** (GET weatherforecast)

Expected: **200 OK** with weather data

## ? Expected Failures (Security Validation)

### Test A: Direct API Call with BFF Token
Run **Step 1.1**

Expected: **401 Unauthorized** ?
(BFF token has wrong audience for API)

### Test B: Exchange with API Client
Run **Step 3.2**

Expected: **Error: "Client is not within the token audience"** ?
(Only BFF can exchange its own tokens)

## ?? If Something Doesn't Work

### Keycloak Not Ready Yet
```bash
# Check Keycloak health
docker ps --filter "name=keycloak"

# Should show: Up XX seconds (healthy)
# If "health: starting", wait 30 more seconds
```

### Token Exchange Still Fails
```bash
# Re-import realm configuration
cd D:\github\poc
docker-compose down
docker-compose up -d

# Wait 60 seconds for startup
```

### User Claims Missing
Check that `.keycloak/realms/poc-realm.json` has the `email` and `profile` client scopes defined in the `clientScopes` array (not just referenced in `defaultClientScopes`).

## ?? Success Indicators

When everything works correctly:

| Test | Status | Evidence |
|------|--------|----------|
| BFF token has user claims | ? | `email`, `name`, `sub` present in token |
| BFF token has correct audience | ? | `"aud": "bff"` |
| Token exchange succeeds | ? | Returns new token without errors |
| API token has correct audience | ? | `"aud": "api"` |
| API token preserves user claims | ? | Same `sub`, `email`, `name` |
| API accepts exchanged token | ? | 200 OK from /weatherforecast |
| API rejects BFF token | ? | 401 Unauthorized |
| API client can't exchange BFF token | ? | Error: "Client is not within the token audience" |

## ?? What We Fixed

### Before
- ? BFF tokens had no user claims (`sub`, `email`, `name` missing)
- ? Token exchange failed: "Requested audience not available: api"

### After
- ? BFF tokens have full user claims
- ? Token exchange works without explicit scope
- ? API tokens have user claims
- ? Audience security pattern enforced

## ?? When All Tests Pass

You now have a working BFF-to-API token exchange with:
- ? Proper audience isolation
- ? User context preservation
- ? Security pattern enforcement
- ? Clean token exchange API

Next: Implement this in your `TokenExchangeTransform.cs` for YARP!
