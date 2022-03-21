#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Procedure.Api/Procedure.Api.csproj", "Procedure.Api/"]
RUN dotnet restore "Procedure.Api/Procedure.Api.csproj"
COPY . .
WORKDIR "/src/Procedure.Api"
RUN dotnet build "Procedure.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Procedure.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Procedure.Api.dll"]