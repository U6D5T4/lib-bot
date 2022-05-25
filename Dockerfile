#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apt-get update
RUN apt-get -y install gss-ntlmssp

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["LibBot/LibBot.csproj", "src/"]
COPY [".git", "src/"]
RUN dotnet restore "src/LibBot.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "LibBot/LibBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LibBot/LibBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibBot.dll"]