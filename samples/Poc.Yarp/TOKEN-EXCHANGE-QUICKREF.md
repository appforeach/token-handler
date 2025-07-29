# Token Exchange Quick Reference

## The Security Pattern

```
???????????????????????????????????????????????????????????????
?  User Authentication Flow with Audience Isolation           ?
???????????????????????????????????????????????????????????????

1. User ? BFF: Login
                ?
2. BFF ? Keycloak: Get token
                ?
3. Keycloak ? BFF: Token with "aud": ["bff"]
                ?
4. User ? BFF: Request (with BFF token)
                ?
5. BFF ? Keycloak: Exchange token for API
                ?
6. Keycloak checks: "Is BFF allowed to get API token?"
                ?
7. Keycloak ? BFF: New token with "aud": ["api"]
                ?
8. BFF ? API: Request (with API token)
                ?
9. API validates: "aud" must be ["api"] ?
                ?
10. API ? BFF: Response
                ?
11. BFF ? User: Final response
```

## Token Comparison

### BFF Token (Original)
```json
{
  "aud": ["bff"],           // ? ONLY bff
  "sub": "user-uuid",
  "azp": "bff",
  "scope": "",
  "jti": "original-jti",
  "iat": 1770378623,
  "exp": 1770379223
}
```

### API Token (Exchanged)
```json
{
  "aud": ["api"],           // ? Changed to api
  "sub": "user-uuid",       // ? Same user
  "azp": "bff",             // ? Original client
  "scope": "",
  "jti": "new-jti",         // ? New token ID
  "iat": 1770378650,        // ? New issue time
  "exp": 1770379250
}
```

## HTTP Requests

### 1. Get BFF Token
```http
POST http://localhost:8080/realms/poc/protocol/openid-connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=password
&client_id=bff
&client_secret=your-client-secret-here
&username=admin
&password=admin123
```

### 2. Try Direct API Call (Should Fail)
```http
GET http://localhost:5001/weatherforecast
Authorization: Bearer <bff_token>

# Expected: 401 Unauthorized
```

### 3. Exchange Token
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

### 4. API Call with Exchanged Token (Should Succeed)
```http
GET http://localhost:5001/weatherforecast
Authorization: Bearer <exchanged_token>

# Expected: 200 OK
```

## Key Configuration Points

### Keycloak - BFF Client
- ? Remove `api-audience-mapper`
- ? Keep only `bff` audience mapper
- ? Enable service account
- ? Enable `oauth2.token.exchange.grant.enabled`

### Keycloak - API Client
- ? Enable Authorization Services
- ? Create Token Exchange Permission resource
- ? Create bff-client-policy
- ? Link permission to policy

### API - JWT Validation
```csharp
options.Audience = "api";
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateAudience = true,  // CRITICAL
    ValidAudience = "api"     // CRITICAL
};
```

## Common Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| BFF token works against API | BFF has `api` audience | Remove `api-audience-mapper` from BFF client |
| Token exchange fails (403) | Missing authorization policy | Configure token exchange permissions |
| Missing `sub` claim | Client credentials or wrong grant | Use password grant with proper scopes |
| Token exchange returns minimal claims | Original token has no scopes | Add default client scopes (profile, email) |

## Security Benefits

? **Privilege Separation**: Different tokens for different services
? **Defense in Depth**: API validates audience even if BFF is compromised
? **Audit Trail**: Each exchange creates new token with new `jti`
? **Limited Blast Radius**: Stolen BFF token cannot access backend
? **Zero Trust**: Each service validates independently

## Quick Test Script

```bash
# 1. Get BFF token
BFF_TOKEN=$(curl -s -X POST http://localhost:8080/realms/poc/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=bff" \
  -d "client_secret=your-client-secret-here" \
  -d "username=admin" \
  -d "password=admin123" | jq -r '.access_token')

echo "BFF Token: $BFF_TOKEN"

# 2. Test direct API call (should fail)
curl -s -X GET http://localhost:5001/weatherforecast \
  -H "Authorization: Bearer $BFF_TOKEN" \
  -w "\nStatus: %{http_code}\n"

# 3. Exchange token
API_TOKEN=$(curl -s -X POST http://localhost:8080/realms/poc/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=urn:ietf:params:oauth:grant-type:token-exchange" \
  -d "client_id=bff" \
  -d "client_secret=your-client-secret-here" \
  -d "subject_token=$BFF_TOKEN" \
  -d "subject_token_type=urn:ietf:params:oauth:token-type:access_token" \
  -d "requested_token_type=urn:ietf:params:oauth:token-type:access_token" \
  -d "audience=api" | jq -r '.access_token')

echo "API Token: $API_TOKEN"

# 4. Test API call with exchanged token (should succeed)
curl -s -X GET http://localhost:5001/weatherforecast \
  -H "Authorization: Bearer $API_TOKEN" \
  -w "\nStatus: %{http_code}\n"
```

## See Also

- `AUDIENCE-SECURITY-PATTERN.md` - Detailed security architecture
- `KEYCLOAK-AUDIENCE-CONFIG.md` - Step-by-step Keycloak setup
- `TOKEN-EXCHANGE-NOTES.md` - Complete token exchange documentation
- `Test.Yarp.http` - Comprehensive test suite
