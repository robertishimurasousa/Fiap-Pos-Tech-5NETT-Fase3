FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
COPY src/Escola.Api/Escola.Api.csproj src/Escola.Api/
RUN dotnet restore src/Escola.Api/Escola.Api.csproj

COPY src/Escola.Api/ src/Escola.Api/
RUN dotnet publish src/Escola.Api/Escola.Api.csproj --configuration Release --no-restore --output /app/publish -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
LABEL org.opencontainers.image.licenses="MIT"
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
COPY LICENSE ./LICENSE
USER $APP_UID
ENTRYPOINT ["dotnet", "Escola.Api.dll"]
