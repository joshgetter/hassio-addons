ARG BUILD_FROM

# ------Stage 1 Build-------
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /KasaStreamer
COPY /KasaStreamer/ .
RUN dotnet publish -c Release . -o /build

# ------Stage 2 Run-------
FROM $BUILD_FROM
ENV LANG C.UTF-8

# Copy data for add-on
COPY run.sh /
RUN chmod a+x /run.sh

# Copy dotnet app
COPY --from=build-env /build /app

# Setup dotnet runtime
ADD https://dot.net/v1/dotnet-install.sh /dotnet-install.sh
RUN chmod a+x /dotnet-install.sh
RUN /dotnet-install.sh --runtime dotnet --version 5.0.0
ENV PATH="/root/.dotnet:${PATH}"
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Install other dependencies
RUN apk add --no-cache \
    ffmpeg \
    nginx \
    nginx-mod-rtmp

# Nginx setup
COPY nginx/nginx.conf /etc/nginx/nginx.conf
RUN mkdir -p /tmp/streaming/thumbnails
RUN mkdir /tmp/streaming/hls

CMD [ "/run.sh" ]
