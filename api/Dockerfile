FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Ilevora.Domain/Ilevora.Domain.csproj Ilevora.Domain/
COPY Ilevora.Application/Ilevora.Application.csproj Ilevora.Application/
COPY Ilevora.Infrastructure/Ilevora.Infrastructure.csproj Ilevora.Infrastructure/
COPY Ilevora.API/Ilevora.API.csproj Ilevora.API/

RUN dotnet restore Ilevora.API/Ilevora.API.csproj

COPY . .
RUN dotnet publish Ilevora.API/Ilevora.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Ilevora.API.dll"]
