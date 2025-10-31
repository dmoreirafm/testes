FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/BankMore.Web/BankMore.Web.csproj", "src/BankMore.Web/"]
RUN dotnet restore "src/BankMore.Web/BankMore.Web.csproj"
COPY . .
WORKDIR "/src/src/BankMore.Web"
RUN dotnet build "BankMore.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BankMore.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BankMore.Web.dll"]

