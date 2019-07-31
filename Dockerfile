FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY JID/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY JID/. ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine

ENV TZ=America/Sao_Paulo
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "JID.dll"]
