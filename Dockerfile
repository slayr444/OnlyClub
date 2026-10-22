# --- Этап 1: Сборка (Build) ---
# Используем официальный SDK. Если у тебя .NET 8, замени 10.0 на 8.0
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем ТОЛЬКО файлы проектов (.csproj), чтобы скачать пакеты.
# Это кэшируется, и при изменении кода восстановление не будет запускаться заново.
COPY ["OnlyClub.API/OnlyClub.API.csproj", "OnlyClub.API/"]
COPY ["OnlyClub.Application/OnlyClub.Application.csproj", "OnlyClub.Application/"]
COPY ["OnlyClub.Core/OnlyClub.Core.csproj", "OnlyClub.Core/"]
COPY ["OnlyClub.Infrastructure/OnlyClub.Infrastructure.csproj", "OnlyClub.Infrastructure/"]
COPY ["OnlyClub.Adapters/OnlyClub.Adapters.csproj", "OnlyClub.Adapters/"]

# Восстанавливаем зависимости
RUN dotnet restore "OnlyClub.API/OnlyClub.API.csproj"

# Копируем весь исходный код
COPY . .

# Собираем релизную версию
WORKDIR "/src/OnlyClub.API"
RUN dotnet publish "OnlyClub.API.csproj" -c Release -o /app/publish /p:UseAppHost=false


# --- Этап 2: Запуск (Runtime) ---
# Используем облегченный образ ASP.NET Core Runtime (там нет компилятора, только среда запуска)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Открываем порт 8080 (стандартный для .NET 8+)
EXPOSE 8080

# Копируем собранные файлы из первого этапа
COPY --from=build /app/publish .

# Запускаем приложение
ENTRYPOINT ["dotnet", "OnlyClub.API.dll"]