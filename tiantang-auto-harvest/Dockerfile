#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim-amd64 AS build
WORKDIR /src
COPY ["tiantang-auto-harvest/tiantang-auto-harvest.csproj", "tiantang-auto-harvest/"]
RUN dotnet restore "tiantang-auto-harvest/tiantang-auto-harvest.csproj"
COPY . .
WORKDIR "/src/tiantang-auto-harvest"
RUN dotnet build "tiantang-auto-harvest.csproj" -c Release -o /app/build

FROM build AS publish
ARG TARGETPLATFORM
RUN if [ "$TARGETPLATFORM" = "linux/amd64" ]; then \
        RID=linux-x64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm64" ]; then \
        RID=linux-arm64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm/v7" ]; then \
        RID=linux-arm ; \
    fi \
    && dotnet publish "tiantang-auto-harvest.csproj" -c Release -o /app/publish -r $RID 

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "tiantang-auto-harvest.dll"]
