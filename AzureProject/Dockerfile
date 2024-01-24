#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
#For ENV timezone of the image
ENV TZ = America/Chicago

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /src
#Copy more .csproj (libraries) if neccessary 
COPY ["AzureProject/AzureProject.csproj", "AzureProject/"]

RUN dotnet restore "AzureProject/AzureProject.csproj" # if more repositories -s 'https://api.nuget.org/v3/index.json' and more

COPY . .
WORKDIR "/src/AzureProject"
RUN dotnet build "AzureProject.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AzureProject.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AzureProject.dll"] 