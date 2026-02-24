FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["Nuget.config", "./"]
COPY ["src/Shared/SharedKernel/SharedKernel.csproj", "src/Shared/SharedKernel/"]
COPY ["src/Shared/SharedKernel.Application/SharedKernel.Application.csproj", "src/Shared/SharedKernel.Application/"]
COPY ["src/Shared/SharedKernel.Infrastructure/SharedKernel.Infrastructure.csproj", "src/Shared/SharedKernel.Infrastructure/"]
COPY ["src/Shared/SharedKernel.Api/SharedKernel.Api.csproj", "src/Shared/SharedKernel.Api/"]
COPY ["src/Gateway.Api/Gateway.Api.csproj", "src/Gateway.Api/"]
RUN dotnet restore "src/Gateway.Api/Gateway.Api.csproj"
COPY . .
WORKDIR "/src/src/Gateway.Api"
RUN dotnet build "./Gateway.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Gateway.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
