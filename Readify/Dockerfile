# Backend multi-stage Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Readify/Readify.csproj", "Readify/"]
RUN dotnet restore "Readify/Readify.csproj"
COPY . .
WORKDIR /src/Readify
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS="http://+:5005"
EXPOSE 5005
ENTRYPOINT ["dotnet", "Readify.dll"]
