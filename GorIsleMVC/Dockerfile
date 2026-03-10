# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["GorIsleMVC.csproj", "."]
RUN dotnet restore

COPY . .
RUN dotnet publish "GorIsleMVC.csproj" -c Release -o /app/publish --no-restore

# Runtime stage (ImageSharp kullanıldığı için libgdiplus gerekmez)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 80

ENTRYPOINT ["dotnet", "GorIsleMVC.dll"]