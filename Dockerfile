FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["05_SE1933_SportCourtManagement_Backend/05_SE1933_SportCourtManagement_Backend.csproj", "05_SE1933_SportCourtManagement_Backend/"]
RUN dotnet restore "05_SE1933_SportCourtManagement_Backend/05_SE1933_SportCourtManagement_Backend.csproj"

# Copy the rest of the source code
COPY . .

WORKDIR "/src/05_SE1933_SportCourtManagement_Backend"
RUN dotnet build "05_SE1933_SportCourtManagement_Backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "05_SE1933_SportCourtManagement_Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "05_SE1933_SportCourtManagement_Backend.dll"]
