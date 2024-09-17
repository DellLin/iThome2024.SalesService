FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5078

ENV ASPNETCORE_URLS=http://+:5078

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["iThome2024.SalesService.csproj", "./"]
RUN dotnet restore "iThome2024.SalesService.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "iThome2024.SalesService.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "iThome2024.SalesService.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "iThome2024.SalesService.dll"]
