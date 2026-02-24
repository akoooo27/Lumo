FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5100
EXPOSE 5101

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
COPY ["src/Auth/Auth.Api/Auth.Api.csproj", "src/Auth/Auth.Api/"]
RUN dotnet restore "src/Auth/Auth.Api/Auth.Api.csproj"
COPY . .
WORKDIR "/src/src/Auth/Auth.Api"
RUN dotnet build "./Auth.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Auth.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Auth.Api.dll"]
