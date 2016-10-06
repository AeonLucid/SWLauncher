# SWLauncher
Soul Worker launcher that removes the need for a Japanese VPN **when launching the game**.

## How does it work?

 1. You fill in the game path and your hangame username and password. 
 2. Press "Ready To Play!" on "Soulworker Patcher" with "Login using the Patcher" disabled *or don't if you know Japanese*.
 3. Press "Launch".
 4. Play Soul Worker.

## Mkay that's cool, but how?
When you press "Launch" it scrapes japanese proxies from http://gatherproxy.com/proxylist/country/?c=Japan.
It signs into the hangame website **without a proxy** and requests the game start arguments **using a japense proxy**, if a proxy fails, it selects another proxy until it works.

## Credits
Thanks to https://github.com/Miyuyami/SWPatcher for showing the login procedure.
