# Installing Magic Pot Backend

This document describes installing Backend in Ubuntu v22.04. If you use different OS or version - please re-check all commands for correct syntax.

This document assumes that you install Backend to `/var/www/magic-pot-api` folder, and create systemd service named `magicpot-api`. If you want to use different names - please update commands and samples below yourself.

## 1. Prerequisites

1. Install `.NET 8.0 SDK` from [Microsoft website](https://dotnet.microsoft.com/ru-ru/download).
    * Exact version number (8.0.7, 8.0.10...) is not important.
    * Make sure you choose correct version for your OS.
    * There are installation instructions for different OSes there.
2. Install [nginx](https://nginx.org/) (for easier management);
3. Install [certbot](https://certbot.eff.org/) (for free HTTPS certificate).

## 2. Installation

1. Clone git repo to your home folder, using command provided by Github (`git clone...`), `cd` to that folder.
2. Type and run `dotnet test` - after 1-2 minutes you should see green message with number of successful test.
3. Create destination folder and set correct rights:
    * `mkdir /var/www/magicpot-api`
    * `chown www-data:www-data /var/www/magicpot-api/`
4. Create systemd daemon:
    * Create new file with command `sudo nano /etc/systemd/system/magicpot-api.service`; 
    * Paste this into file editor:
        ```
        [Unit]
        Description=Magic Pot API -=MYAPP=-

        [Service]
        WorkingDirectory=/var/www/magicpot-api
        ExecStart=/var/www/magicpot-api/backend
        Restart=always
        RestartSec=10
        TimeoutStopSec=30
        KillMode=process
        KillSignal=SIGTERM
        SyslogIdentifier=magicpot-api
        User=www-data
        Environment=ASPNETCORE_ENVIRONMENT=Production
        Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

        [Install]
        WantedBy=multi-user.target
        ```
    * Save file, quit editor;
    * Enable new service by running `sudo systemctl daemon-reload`.
5. Run `./publish.sh`. \
    It will take about half a minute to build, deploy and start, and then you will see log messages from running backend. There should be no any red (error) lines there. \
    You can quit from log view by pressing Ctrl+C. This will NOT stop the backend - it's still running in background and configured for auto-start on server startup. \
    You can always return back to logs with command `journalctl -fu magicpot-api -n 100`
6. It's time to make backend accessible from Internet.
    * Use you DNS Provider control panel to create new A-record for your domain with name of your desired Backend name (e.g. api.mymagicpotsite.com);
    * Create new website in nginx and use this config:
        ```
        server {
            listen 80;
            listen [::]:80;
            server_name api.mymagicpotsite.com;

            location / {
                proxy_pass         http://localhost:5000;
                proxy_http_version 1.1;
                proxy_set_header   Upgrade $http_upgrade;
                proxy_set_header   Connection keep-alive;
                proxy_set_header   HOST $host;
                proxy_cache_bypass $http_upgrade;
                proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
                proxy_set_header   X-Forwarded-Proto $scheme;
                proxy_set_header   X-Real-IP $remote_addr;
            }
        }
        ```
    * Save and apply Gninx changes;
    * Run `certbot` to create HTTPS Certificate for `api.mymagicpotsite.com` site;
7. Now you may open `https://api.mymagicpotsite.com` - you should see text `Status Code: 404; Not Found` in your browser. Thats OK.
    * Now open `https://api.mymagicpotsite.com/swagger` and you should see REST API documentation. 

## 3. Updates

To update your backend installation, use these 3 steps:

1. Go to folder where Git repo had been cloned to;
2. Run `git pull` to recieve last version;
3. Run `./publish.sh` to rebuild backend and restart it.