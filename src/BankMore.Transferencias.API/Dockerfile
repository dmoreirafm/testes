FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/BankMore.Transferencias.API/BankMore.Transferencias.API.csproj", "src/BankMore.Transferencias.API/"]
COPY ["src/BankMore.Transferencias.Application/BankMore.Transferencias.Application.csproj", "src/BankMore.Transferencias.Application/"]
COPY ["src/BankMore.Transferencias.Domain/BankMore.Transferencias.Domain.csproj", "src/BankMore.Transferencias.Domain/"]
COPY ["src/BankMore.Transferencias.Infrastructure/BankMore.Transferencias.Infrastructure.csproj", "src/BankMore.Transferencias.Infrastructure/"]
COPY ["src/BankMore.Contas.Application/BankMore.Contas.Application.csproj", "src/BankMore.Contas.Application/"]

RUN dotnet restore "src/BankMore.Transferencias.API/BankMore.Transferencias.API.csproj"
COPY . .
WORKDIR "/src/src/BankMore.Transferencias.API"
RUN dotnet build "BankMore.Transferencias.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BankMore.Transferencias.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BankMore.Transferencias.API.dll"]

