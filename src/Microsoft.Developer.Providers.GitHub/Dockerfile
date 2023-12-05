# To enable ssh & remote debugging on app service change the base image to the one below
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0-appservice
# FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0

ARG IMAGE_VERSION

ARG BUILD_DATE

ENV DEVELOPER_API_IMAGE_VERSION=$IMAGE_VERSION

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

ENV AzureWebJobsDisableHomepage=true \
    AzureWebJobsFeatureFlags=EnableHttpProxying

LABEL maintainer="Microsoft" \
    org.opencontainers.image.created=$BUILD_DATE \
    org.opencontainers.image.url="https://github.com/microsoft/developer-platform-github" \
    org.opencontainers.image.source="https://github.com/microsoft/developer-platform-github" \
    org.opencontainers.image.version=$IMAGE_VERSION \
    org.opencontainers.image.vendor="Microsoft" \
    org.opencontainers.image.title="Microsoft Developer Platform GitHub Provider" \
    org.opencontainers.image.description="The Microsoft Developer Platform GitHub Provider." \
    org.opencontainers.image.documentation="https://github.com/microsoft/developer-platform-github" \
    org.opencontainers.image.licenses="MIT"

RUN mkdir -p /home/site/wwwroot

COPY home/site/wwwroot /home/site/wwwroot
