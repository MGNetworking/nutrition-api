FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/NutritionApi.Api/NutritionApi.Api.csproj", "src/NutritionApi.Api/"]
COPY ["src/NutritionApi.Application/NutritionApi.Application.csproj", "src/NutritionApi.Application/"]
COPY ["src/NutritionApi.Domain/NutritionApi.Domain.csproj", "src/NutritionApi.Domain/"]
COPY ["src/NutritionApi.Infrastructure/NutritionApi.Infrastructure.csproj", "src/NutritionApi.Infrastructure/"]
RUN dotnet restore "src/NutritionApi.Api/NutritionApi.Api.csproj"
COPY . .
RUN dotnet build "src/NutritionApi.Api/NutritionApi.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "src/NutritionApi.Api/NutritionApi.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NutritionApi.Api.dll"]
