FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Subly.Api/Subly.Api.csproj", "src/Subly.Api/"]
COPY ["src/Subly.Application/Subly.Application.csproj", "src/Subly.Application/"]
COPY ["src/Subly.Domain/Subly.Domain.csproj", "src/Subly.Domain/"]
COPY ["src/Subly.Infrastructure/Subly.Infrastructure.csproj", "src/Subly.Infrastructure/"]
RUN dotnet restore "src/Subly.Api/Subly.Api.csproj"

COPY . .
RUN dotnet publish "src/Subly.Api/Subly.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
RUN mkdir -p /app/data

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Subly.Api.dll"]
