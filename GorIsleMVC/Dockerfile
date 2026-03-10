# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["GorIsleMVC.csproj", "."]
RUN dotnet restore

COPY . .
RUN dotnet publish "GorIsleMVC.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    libc6-dev \
    fontconfig \
    && ln -s /usr/lib/libgdiplus.so /usr/lib/gdiplus.dll \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 80

ENTRYPOINT ["dotnet", "GorIsleMVC.dll"]