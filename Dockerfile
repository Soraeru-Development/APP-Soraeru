FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY nuget.config Directory.Build.props Directory.Build.targets ./
COPY src/Soraeru.Api/Soraeru.Api.csproj src/Soraeru.Api/
COPY src/Soraeru.Application/Soraeru.Application.csproj src/Soraeru.Application/
COPY src/Soraeru.Infrastructure/Soraeru.Infrastructure.csproj src/Soraeru.Infrastructure/

RUN dotnet restore src/Soraeru.Api/Soraeru.Api.csproj

COPY src/Soraeru.Api/ src/Soraeru.Api/
COPY src/Soraeru.Application/ src/Soraeru.Application/
COPY src/Soraeru.Infrastructure/ src/Soraeru.Infrastructure/

RUN dotnet publish src/Soraeru.Api/Soraeru.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN mkdir -p /app/data
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Soraeru.Api.dll"]
