# ---------- Etapa de runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

# ---------- Etapa de build ----------
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copiem doar fișierul proiectului pentru restore rapid
COPY ["Licenta3.csproj", "./"]
RUN dotnet restore "Licenta3.csproj"

# Copiem tot proiectul
COPY . .

# Publicăm aplicația
RUN dotnet publish "Licenta3.csproj" -c Release -o /app/publish

# ---------- Etapa finală ----------
FROM base AS final
WORKDIR /app

# Copiem aplicația publicată din etapa de build
COPY --from=build /app/publish .

# Setăm entrypoint pentru rulare
ENTRYPOINT ["dotnet", "Licenta3.dll"]