# Dockerfile para build RELATIVO a la carpeta Cocktail.back (.NET 9.0)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiamos el csproj (asumiendo que estamos DENTRO de Cocktail.back)
COPY Cocktail.back.csproj ./
RUN dotnet restore Cocktail.back.csproj

# Copiamos todo lo demás
COPY . ./

# Publicar
RUN dotnet publish Cocktail.back.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime en .NET 9.0
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Puerto estándar para .NET 9 (Railway lo sobreescribirá con $PORT si es necesario)
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Cocktail.back.dll"]
