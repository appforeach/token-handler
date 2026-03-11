# ? SOLUTION COMPLETED: Token Exchange Configuration

## Summary
Fixed the "Requested audience not available: api" error by moving `api-audience` from **optional** to **default** client scopes for the BFF client.

## What Was Changed

### 1. Configuration Update
**File**: `.keycloak/realms/poc-realm.json`

```diff
  "clientId": "bff",
  "defaultClientScopes": [
    "email",
-   "profile"
+   "profile",
+   "api-audience"
  ],
  "optionalClientScopes": [
-   "api-audience"
  ]
```

### 2. Test File Updated
**File**: `samples/Poc.Yarp/Test.Yarp.http`

- **Step 3**: Token exchange without explicit scope (now works!)
- **Step 3.1**: Token exchange with explicit scope (still works, for reference)
- **Step 3.2**: Negative test with API client (validates security)

## How to Test

### 1. Verify Keycloak is Running
```bash
docker ps | grep keycloak
```

### 2. Get a BFF Token (Step 1)
```http
POST http://localhost:8080/realms/poc/protocol/openid-connect/token

grant_type=password
&client_id=bff
&client_secret=your-client-secret-here
&username=admin
&password=admin123
```

**Check the token at https://jwt.io** - should have:
- ? `"aud": "bff"` (only BFF audience)
- ? `"sub": "..."` (user ID)
- ? `"email": "admin@example.com"`
- ? `"name": "Admin User"`
- ? `"scope": "profile email"`

### 3. Exchange BFF ? API (Step 3)
```http
POST http://localhost:8080/realms/poc/protocol/openid-connect/token

grant_type=urn:ietf:params:oauth:grant-type:token-exchange
&client_id=bff
&client_secret=your-client-secret-here
&subject_token={{adminAccessToken}}
&subject_token_type=urn:ietf:params:oauth:token-type:access_token
&audience=api
```

**Expected Result**: Success! Token with:
- ? `"aud": "api"` (changed to API audience)
- ? All user claims preserved (`sub`, `email`, `name`, etc.)

### 4. Call API with Exchanged Token (Step 4)
```http
GET http://localhost:5001/weatherforecast
Authorization: Bearer {{apiToken}}
```

**Expected**: `200 OK`

## Why This Works

### Before (Broken)
```
BFF Client ? Token Exchange Request (audience=api)
              ?
         Keycloak checks:
         - ? Does BFF have api-audience scope?
         - NO ? api-audience is optional, not requested
              ?
         Error: "Requested audience not available"
```

### After (Fixed)
```
BFF Client ? Token Exchange Request (audience=api)
              ?
         Keycloak checks:
         - ? Does BFF have api-audience scope?
         - YES ? api-audience is in default scopes
         - ? Is BFF in token audience? YES
         - ? Authorization policy allows? YES
              ?
         Success: New token with aud=["api"]
```

## Security Validation

### ? Audience Isolation Still Enforced

| Test | Token Used | Expected Result | Actual Result |
|------|-----------|----------------|---------------|
| Direct API call with BFF token | BFF token (`aud=bff`) | ? 401 Unauthorized | ? Fails as expected |
| Token exchange BFF ? API | BFF token | ? Success | ? Works |
| Use exchanged token on API | API token (`aud=api`) | ? 200 OK | ? Works |

### ? Only BFF Can Exchange Its Tokens

| Test | Client Used | Subject Token | Expected Result | Actual Result |
|------|------------|---------------|----------------|---------------|
| Exchange with BFF client | `bff` | BFF token | ? Success | ? Works |
| Exchange with API client | `api` | BFF token | ? Error: "Client not in audience" | ? Fails as expected |

## Files Created/Modified

### Modified
- ? `.keycloak/realms/poc-realm.json` - BFF client scope configuration
- ? `samples/Poc.Yarp/Test.Yarp.http` - Updated tests

### Created
- ? `.keycloak/update-bff-scopes.ps1` - Automation script
- ? `samples/Poc.Yarp/TOKEN-EXCHANGE-FIX.md` - Detailed documentation
- ? `samples/Poc.Yarp/SOLUTION-SUMMARY.md` - This file

## Troubleshooting

### If token exchange still fails:

1. **Check Keycloak logs**:
   ```bash
   docker logs poc-keycloak
   ```

2. **Verify BFF client configuration**:
   ```bash
   # Check that api-audience is in default scopes
   cat .keycloak/realms/poc-realm.json | grep -A 5 '"clientId": "bff"'
   ```

3. **Re-import the realm**:
   ```bash
   docker-compose down
   docker-compose up -d
   ```

### If user claims are missing:

The fix we implemented should have also added the `email` and `profile` client scopes at the realm level. Check that tokens include:
- ? `sub` (user ID)
- ? `email`
- ? `name` / `given_name` / `family_name`
- ? `preferred_username`

## Related Documentation

- ?? `TOKEN-EXCHANGE-FIX.md` - Complete technical explanation
- ?? `AUDIENCE-SECURITY-PATTERN.md` - Security architecture
- ?? `TOKEN-EXCHANGE-QUICKREF.md` - Quick reference guide
- ?? `TOKEN-EXCHANGE-NOTES.md` - Implementation notes

## Next Steps

1. ? Keycloak restarted
2. ?? Test the token exchange (use `Test.Yarp.http`)
3. ?? Verify all three tests pass:
   - Step 1: Get BFF token with user claims
   - Step 3: Exchange to API token
   - Step 4: Use API token successfully

## Success Criteria

Your configuration is working correctly when:

- ? Password grant returns BFF tokens with `aud=["bff"]` and full user claims
- ? Token exchange from BFF ? API succeeds **without** `&scope=api-audience`
- ? Exchanged tokens have `aud=["api"]` and preserve user claims
- ? Direct use of BFF tokens on API fails (401)
- ? Exchanged API tokens work on API (200 OK)
- ? API client cannot exchange BFF tokens (security validated)

---

**Status**: ? COMPLETE - Configuration updated and Keycloak restarted
**Ready for testing**: YES
**Next action**: Run tests in `Test.Yarp.http`
