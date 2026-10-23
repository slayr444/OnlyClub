# --- Этап 1: Сборка (Build) ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем вообще всё (благодаря .dockerignore мусор вроде bin/obj и .git не попадет)
COPY . .

# Находим главный проект (Web API) и публикуем его.
# Скрипт ищет все .csproj, игнорирует библиотеки и берет первый попавшийся основной проект.
RUN PROJECT_FILE=$(find . -name "*.csproj" -not -path "*/OnlyClub.Application/*" -not -path "*/OnlyClub.Core/*" -not -path "*/OnlyClub.Infrastructure/*" -not -path "*/OnlyClub.Adapters/*" | head -n 1) && \
    echo "==================================================" && \
    echo "СБОРКА ПРОЕКТА: $PROJECT_FILE" && \
    echo "==================================================" && \
    dotnet publish "$PROJECT_FILE" -c Release -o /app/publish /p:UseAppHost=false


# --- Этап 2: Запуск (Runtime) ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

EXPOSE 8080

# Копируем собранные файлы
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "OnlyClub.API.dll"]
