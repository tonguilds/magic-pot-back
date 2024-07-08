dotnet build bot/bot.csproj -c Release && \
  sudo systemctl stop magicpot-bot && \
  sudo dotnet publish bot/bot.csproj -c Release -o /var/www/magicpot-bot && \
  sudo systemctl start magicpot-bot && \
  journalctl -fu magicpot-bot -n 10 --no-hostname
