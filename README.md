# Magic Pot Backend

Backend service for Magic Pot.

Tech stack: C# and net8.0, uses sqlite.

For production: It is recommended to use nginx and LetsEncrypt (or an alternative) for simplicity and versatility.


## Description

Main features:
* Holds pots (transactions, users) database;
* Uses Pinata to store Pot images;
* Provides a REST API for front-end to view and manage Pots by user;
* Tracks blockchain transactions with tokens transfer to Pot addresses;
* Sends notifications to the configured channel about Pot events;
* Distributes prizes.

Additional features:
* Separate `/health` page for putting backend into automated monitoring;
* Automatic DB backups every several hours and immediately after creating a new pot;
* Second process used for blockchain tracking to minimize memory leaks (restarted every several hours).

## Installation

See [separate document](Installation.md).

## Configuration

Make sure you have prepared:

* Telegram Bot token (from BotFather): for validating users, and for publishing info into channel/chat;
* Pinata credentials (from [pinata.cloud](https://www.pinata.cloud/)): for storing pot cover images.

Edit the `appsettings.json` file. A short help/description for each setting is included inside.

⚠️ Important note: Backend may run on both testnet and mainnet - this is controlled by the `UseMainnet` setting. You MUST manually delete existing database when switching between networks - otherwise backend will refuse to start.