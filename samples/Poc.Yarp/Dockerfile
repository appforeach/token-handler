FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY samples/Poc.Yarp/Poc.Yarp.csproj Poc.Yarp/
COPY src/Poc.TokenHandler ./Poc.TokenHandler
RUN dotnet restore "Poc.Yarp/Poc.Yarp.csproj"
COPY samples/Poc.Yarp/. Poc.Yarp/
WORKDIR "/src/Poc.Yarp"
RUN dotnet build "Poc.Yarp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Poc.Yarp.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Poc.Yarp.dll"]