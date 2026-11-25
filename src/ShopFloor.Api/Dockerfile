# Stage names follow the Visual Studio convention: in Debug, VS builds only the
# "base" stage and runs the app from your local build output (fast mode)

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
USER $APP_UID
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ShopFloor.Domain/ShopFloor.Domain.csproj src/ShopFloor.Domain/
COPY src/ShopFloor.Api/ShopFloor.Api.csproj src/ShopFloor.Api/
RUN dotnet restore src/ShopFloor.Api/ShopFloor.Api.csproj

COPY src/ src/
RUN dotnet publish src/ShopFloor.Api/ShopFloor.Api.csproj --configuration Release --no-restore --output /app/publish

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ShopFloor.Api.dll"]
