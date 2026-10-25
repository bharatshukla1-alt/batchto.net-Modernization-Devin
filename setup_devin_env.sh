#!/usr/bin/env bash
# ==============================================================================
# setup_devin_env.sh — Bootstraps .NET 8 SDK and ICU in Devin's Ubuntu Sandbox
# ==============================================================================
set -e

echo "================================================================================"
echo "    Mainframe Batch to .NET 8 Modernization: Devin Environment Setup           "
echo "================================================================================"

# Check if dotnet is already installed
if command -v dotnet >/dev/null 2>&1; then
    DOTNET_VER=$(dotnet --version)
    echo "[INFO] .NET CLI already found: version $DOTNET_VER"
    if [[ "$DOTNET_VER" =~ ^[89]\. ]]; then
        echo "[SUCCESS] .NET 8 or 9 SDK is already active. Skipping package installation."
        exit 0
    fi
fi

echo "[INFO] Installing .NET 8 SDK and dependencies on Ubuntu..."

# Download Microsoft signing key and repository package
if [ ! -f "/etc/apt/sources.list.d/microsoft-prod.list" ]; then
    UBUNTU_CODENAME=$(lsb_release -cs 2>/dev/null || echo "jammy")
    wget -q https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    rm -f packages-microsoft-prod.deb
fi

# Update package lists and install dotnet-sdk-8.0 and libicu
sudo apt-get update -qq
sudo apt-get install -y -qq dotnet-sdk-8.0 libicu-dev build-essential

echo "[SUCCESS] Installed .NET SDK:"
dotnet --version

echo "[SUCCESS] Environment is fully configured for Devin."
