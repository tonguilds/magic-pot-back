dotnet build backend/backend.csproj -c Release && \
  sudo systemctl stop magicpot-api && \
  sudo dotnet publish backend/backend.csproj -c Release -o /var/www/magicpot-api && \
  sudo systemctl start magicpot-api && \
  journalctl -fu magicpot-api -n 10 --no-hostname
