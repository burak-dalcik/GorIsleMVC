# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["GorIsleMVC/GorIsleMVC.csproj", "GorIsleMVC/"]
RUN dotnet restore "GorIsleMVC/GorIsleMVC.csproj"

COPY GorIsleMVC/ GorIsleMVC/
WORKDIR /src/GorIsleMVC
RUN dotnet publish "GorIsleMVC.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# System.Drawing.Common için Linux'ta libgdiplus gerekir
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Uygulama 80 portunda dinlesin
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "GorIsleMVC.dll"]
