#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["RedisStatusPage.Notification/RedisStatusPage.Notification.csproj", "RedisStatusPage.Notification/"]
COPY ["RedisStatusPage.Core/RedisStatusPage.Core.csproj", "RedisStatusPage.Core/"]
RUN dotnet restore "RedisStatusPage.Notification/RedisStatusPage.Notification.csproj"
COPY . .
WORKDIR "/src/RedisStatusPage.Notification"
RUN dotnet build "RedisStatusPage.Notification.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RedisStatusPage.Notification.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY wait-for-it.sh .
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RedisStatusPage.Notification.dll"]