# ---------- Etapa de runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

# Variabile necesare pentru ASP.NET Core și Railway
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_URLS=http://+:${PORT:-80}

# ---------- Etapa de build ----------
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copiem fișierul proiectului pentru restore rapid
COPY ["Licenta3.csproj", "./"]
RUN dotnet restore "Licenta3.csproj"

# Copiem restul codului sursă
COPY . .

# Publicăm aplicația într-un folder dedicat
RUN dotnet publish "Licenta3.csproj" -c Release -o /app/publish

# ---------- Etapa finală ----------
FROM base AS final
WORKDIR /app

# Copiem aplicația publicată din etapa de build
COPY --from=build /app/publish .

# Entry point pentru container
ENTRYPOINT ["dotnet", "Licenta3.dll"]
