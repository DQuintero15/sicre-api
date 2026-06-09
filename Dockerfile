FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Sicre.Api/Sicre.Api.csproj", "Sicre.Api/"]
RUN dotnet restore "Sicre.Api/Sicre.Api.csproj"

COPY . .
WORKDIR "/src/Sicre.Api"
RUN dotnet publish "Sicre.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

RUN apt-get update && \
    apt-get install -y tzdata curl && \
    rm -rf /var/lib/apt/lists/*

ENV TZ=America/Bogota

EXPOSE 9001

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Sicre.Api.dll"]
