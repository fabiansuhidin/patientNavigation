#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Procedure.Consumer/Procedure.Consumer.csproj", "Procedure.Consumer/"]
RUN dotnet restore "Procedure.Consumer/Procedure.Consumer.csproj"
COPY . .
WORKDIR "/src/Procedure.Consumer"
RUN dotnet build "Procedure.Consumer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Procedure.Consumer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Procedure.Consumer.dll"]