#!/usr/bin/env bash
#
# Cleaner installer for macOS and Linux.
# Downloads the latest Native AOT binary for your platform into ~/.cleaner/bin and adds it to PATH.
#
#   curl -fsSL https://raw.githubusercontent.com/suxrobGM/cleaner-cli/main/scripts/install.sh | bash
#
set -euo pipefail

REPO="suxrobGM/cleaner-cli"
INSTALL_DIR="${HOME}/.cleaner/bin"
BIN_NAME="cleaner"

info()  { printf '\033[0;36m%s\033[0m\n' "$*"; }
error() { printf '\033[0;31merror:\033[0m %s\n' "$*" >&2; exit 1; }

command -v curl >/dev/null 2>&1 || error "curl is required but was not found."
command -v tar  >/dev/null 2>&1 || error "tar is required but was not found."

# Detect platform and map it to the release runtime identifier.
case "$(uname -s)" in
  Linux)  rid_os="linux" ;;
  Darwin) rid_os="osx" ;;
  *) error "Unsupported OS: $(uname -s)" ;;
esac

case "$(uname -m)" in
  x86_64|amd64)  rid_arch="x64" ;;
  arm64|aarch64) rid_arch="arm64" ;;
  *) error "Unsupported architecture: $(uname -m)" ;;
esac

rid="${rid_os}-${rid_arch}"
info "Detected platform: ${rid}"

# Resolve the matching asset from the latest release.
asset_url="$(curl -fsSL "https://api.github.com/repos/${REPO}/releases/latest" \
  | grep -o '"browser_download_url": *"[^"]*"' \
  | sed 's/.*"browser_download_url": *"\([^"]*\)".*/\1/' \
  | grep -E "${rid}\.tar\.gz$" \
  | head -n1 || true)"

[ -n "${asset_url}" ] || error "No release asset found for ${rid}. See https://github.com/${REPO}/releases"

# Download and extract into a temporary directory.
tmp="$(mktemp -d)"
trap 'rm -rf "${tmp}"' EXIT
info "Downloading ${asset_url##*/}"
curl -fsSL "${asset_url}" -o "${tmp}/cleaner.tar.gz"
tar -xzf "${tmp}/cleaner.tar.gz" -C "${tmp}"

[ -f "${tmp}/${BIN_NAME}" ] || error "The downloaded archive did not contain a '${BIN_NAME}' binary."

# Install into ~/.cleaner/bin.
mkdir -p "${INSTALL_DIR}"
install -m 0755 "${tmp}/${BIN_NAME}" "${INSTALL_DIR}/${BIN_NAME}"
info "Installed to ${INSTALL_DIR}/${BIN_NAME}"

# Add ~/.cleaner/bin to PATH when needed.
add_to_path() {
  case ":${PATH}:" in
    *":${INSTALL_DIR}:"*) return ;;
  esac

  local profile
  case "$(basename "${SHELL:-}")" in
    zsh)  profile="${ZDOTDIR:-$HOME}/.zshrc" ;;
    bash) [ "${rid_os}" = "osx" ] && profile="${HOME}/.bash_profile" || profile="${HOME}/.bashrc" ;;
    *)    profile="${HOME}/.profile" ;;
  esac

  if [ -f "${profile}" ] && grep -qF '.cleaner/bin' "${profile}"; then
    return
  fi

  printf '\n# Added by the cleaner installer\nexport PATH="$HOME/.cleaner/bin:$PATH"\n' >> "${profile}"
  info "Added ${INSTALL_DIR} to PATH in ${profile} - restart your shell or run: source ${profile}"
}
add_to_path

printf '\033[0;32m%s\033[0m\n' "Done. Run 'cleaner' to get started."
