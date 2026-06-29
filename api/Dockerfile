FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AgroMesh.Domain/AgroMesh.Domain.csproj AgroMesh.Domain/
COPY AgroMesh.Application/AgroMesh.Application.csproj AgroMesh.Application/
COPY AgroMesh.Infrastructure/AgroMesh.Infrastructure.csproj AgroMesh.Infrastructure/
COPY AgroMesh.API/AgroMesh.API.csproj AgroMesh.API/

RUN dotnet restore AgroMesh.API/AgroMesh.API.csproj

COPY . .
RUN dotnet publish AgroMesh.API/AgroMesh.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AgroMesh.API.dll"]
