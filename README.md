# NetworkSpeedMonitor
A tool used to get the current Download and upload speed from a router with SNMP and send it to a MQTT broker.

### Example (Used with a advanced tomato router, the -sc, -su and -sd falgs are diffrent for diffrent routers.)
`dotnet NetworkSpeedMonitor.dll -ss 192.168.9.1 -sc rocommunity -su 1.3.6.1.2.1.2.2.1.16.10 -sd 1.3.6.1.2.1.2.2.1.10.10 -ms 192.168.9.15 -mt NetworkSpeedMonitor`
