FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Poc.Api.csproj .
RUN dotnet restore "Poc.Api.csproj"
COPY . .
WORKDIR "/src/Poc.Api"
RUN dotnet build "../Poc.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "../Poc.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Poc.Api.dll"] 