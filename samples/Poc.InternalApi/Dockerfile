FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Poc.InternalApi.csproj .
RUN dotnet restore "Poc.InternalApi.csproj"
COPY . .
WORKDIR "/src/Poc.InternalApi"
RUN dotnet build "../Poc.InternalApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "../Poc.InternalApi.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Poc.InternalApi.dll"]
