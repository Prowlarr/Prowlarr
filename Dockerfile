# syntax=docker/dockerfile:1
ARG BASE_IMAGE=ghcr.io/linuxserver/prowlarr:latest
FROM ${BASE_IMAGE}

ARG BUILD_DATE
ARG VERSION
ARG PROWLARR_BRANCH=master
ARG PACKAGE_AUTHOR="github.com/actuallyevan/Prowlarr"
ARG TARGETARCH

LABEL org.opencontainers.image.created="${BUILD_DATE}" \
  org.opencontainers.image.source="${PACKAGE_AUTHOR}" \
  org.opencontainers.image.version="${VERSION}"

# Copy the pre-compiled C# binaries and UI directly from the build context
COPY _artifacts/linux-musl-x64/net8.0/Prowlarr /tmp/prowlarr-x64
COPY _artifacts/linux-musl-arm64/net8.0/Prowlarr /tmp/prowlarr-arm64

RUN mkdir -p /app/prowlarr/bin && \
  if [ "$TARGETARCH" = "amd64" ]; then \
    cp -r /tmp/prowlarr-x64/* /app/prowlarr/bin/; \
  elif [ "$TARGETARCH" = "arm64" ]; then \
    cp -r /tmp/prowlarr-arm64/* /app/prowlarr/bin/; \
  else \
    echo "Unsupported TARGETARCH: $TARGETARCH" >&2; exit 1; \
  fi && \
  rm -rf /tmp/prowlarr-x64 /tmp/prowlarr-arm64 && \
  echo -e "UpdateMethod=docker\nBranch=${PROWLARR_BRANCH}\nPackageVersion=${VERSION:-LocalBuild}\nPackageAuthor=${PACKAGE_AUTHOR}" > /app/prowlarr/package_info && \
  printf "Linuxserver.io version: ${VERSION}\nBuild-date: ${BUILD_DATE}" > /build_version && \
  echo "**** cleanup ****" && \
  rm -rf \
    /app/prowlarr/bin/Prowlarr.Update
