# Estágio de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar os arquivos csproj e restaurar dependências
COPY ["src/Presentation/Presentation.csproj", "src/Presentation/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
RUN dotnet restore "src/Presentation/Presentation.csproj"

# Copiar todo o código fonte
COPY . .

# Compilar o projeto
WORKDIR /app/src/Presentation
RUN dotnet build "Presentation.csproj" -c Release -o /app/build

# Estágio de publicação
FROM build AS publish
RUN dotnet publish "Presentation.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio final com runtime ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Configurar variáveis de ambiente
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Copiar arquivos de configuração
COPY ["src/Presentation/appsettings.json", "./"]
COPY ["src/Presentation/appsettings.Production.json", "./"]

# Expor porta
EXPOSE 80

# Iniciar a aplicação
ENTRYPOINT ["dotnet", "Presentation.dll"]