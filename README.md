# Tether

A Dalamud plugin to provide a temporary chats into game.\
It allows you to create chat with users and talk to them in locked instances!

## Installation
1. Go to Experimental Tab in Dalamud Settings (`/xlplugins`), and put `https://raw.githubusercontent.com/nesforia/tether/refs/heads/master/tether.json` into Custom Plugin Repositories tab.
2. Save it, and in `/xlplugins` look for `Tether`
3. Install it.

## Usage
You can create chat by clicking RMB on player in instance or in your friendlist.\
If they have the same plugin, they gonna get a request from you.\
Accepting it gonna create a chat! If you have chat, you can invite people to the chat in the same way.

## Development
You need [Tether Backend Service](https://github.com/nesforia/tether-api) to properly set up development environment. Copy `./config/Secrets.example.cs` rename it to `Secrets.cs` and configure `URL` to match with TBS.
