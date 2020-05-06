FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY ["PrintLog.Web/PrintLog.Web.csproj", "PrintLog.Web/"]

RUN dotnet restore "PrintLog.Web/PrintLog.Web.csproj"
COPY . .
WORKDIR "/src/PrintLog.Web"
RUN dotnet build "PrintLog.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PrintLog.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PrintLog.Web.dll"]