FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/BankMore.Contas.API/BankMore.Contas.API.csproj", "src/BankMore.Contas.API/"]
COPY ["src/BankMore.Contas.Application/BankMore.Contas.Application.csproj", "src/BankMore.Contas.Application/"]
COPY ["src/BankMore.Contas.Domain/BankMore.Contas.Domain.csproj", "src/BankMore.Contas.Domain/"]
COPY ["src/BankMore.Contas.Infrastructure/BankMore.Contas.Infrastructure.csproj", "src/BankMore.Contas.Infrastructure/"]

RUN dotnet restore "src/BankMore.Contas.API/BankMore.Contas.API.csproj"
COPY . .
WORKDIR "/src/src/BankMore.Contas.API"
RUN dotnet build "BankMore.Contas.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BankMore.Contas.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BankMore.Contas.API.dll"]

