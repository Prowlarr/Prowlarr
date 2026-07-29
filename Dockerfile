# syntax=docker/dockerfile:1
ARG BASE_IMAGE=ghcr.io/linuxserver/prowlarr:latest

FROM scratch AS binaries
ARG TARGETARCH
COPY _artifacts/linux-musl-x64/net8.0/Prowlarr /amd64
COPY _artifacts/linux-musl-arm64/net8.0/Prowlarr /arm64

FROM ${BASE_IMAGE}

ARG BUILD_DATE
ARG VERSION
ARG PROWLARR_BRANCH=master
ARG PACKAGE_AUTHOR="github.com/actuallyevan/Prowlarr"
ARG TARGETARCH

LABEL org.opencontainers.image.created="${BUILD_DATE}" \
  org.opencontainers.image.source="${PACKAGE_AUTHOR}" \
  org.opencontainers.image.version="${VERSION}"

RUN rm -rf /app/prowlarr/bin/*

COPY --chmod=755 --from=binaries /${TARGETARCH}/. /app/prowlarr/bin/

RUN echo -e "UpdateMethod=docker\nBranch=${PROWLARR_BRANCH}\nPackageVersion=${VERSION:-LocalBuild}\nPackageAuthor=${PACKAGE_AUTHOR}" > /app/prowlarr/package_info && \
  printf "Linuxserver.io version: ${VERSION}\nBuild-date: ${BUILD_DATE}" > /build_version && \
  echo "**** cleanup ****" && \
  rm -rf \
    /app/prowlarr/bin/Prowlarr.Update
