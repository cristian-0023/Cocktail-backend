# ---------- Etapa de compilación ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Cocktail.back.csproj", "./"]
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

# ---------- Etapa de ejecución ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Railway usa la variable PORT, configuramos ASP.NET para escucharla
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}
EXPOSE ${PORT:-8080}

ENTRYPOINT ["dotnet", "Cocktail.back.dll"]

