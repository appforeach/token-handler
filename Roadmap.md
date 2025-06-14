# POC Application


## Milestones


### Setup Api use [Authorization]

done


### Setup Api project to use Keycloak authentication


### Unify Poc.TokehAuthProxy and Poc.YarpProxy into a single project

what is 
- authority
- audience

check builder.Services.AddDistributedMemoryCache(); // For development. In production, use Redis or SQL Server

Resources: https://medium.com/@phat.tan.nguyen/oauth-2-0-the-client-credentials-grant-type-with-keycloak-2debb88a1c70



Realm OpenID Endpoint Configuration: http://localhost:8080/realms/poc/.well-known/openid-configuration
 

curl -X POST "https://<keycloak-server>/auth/realms/<realm-name>/protocol/openid-connect/token" \
     -H "Content-Type: application/x-www-form-urlencoded" \
     -d "client_id=<client-id>" \
     -d "client_secret=<client-secret>" \
     -d "grant_type=client_credentials"



curl -X POST "https://<keycloak-server>/auth/realms/<realm-name>/protocol/openid-connect/token" \
     -H "Content-Type: application/x-www-form-urlencoded" \
     -d "client_id=<client-id>" \
     -d "username=<username>" \
     -d "password=<password>" \
     -d "grant_type=password"



### Setup YARP reverse proxy to forward requests to the Api



