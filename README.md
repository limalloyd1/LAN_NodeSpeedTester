# LAN_NodeSpeedTester
Internet speed tester that uses the socket namespace in a dotnet console app (C#) to test internet download speed. To use set serverIP and clientIP for different methods. Note: Console app opens port 11111 for tcp handshake. A secondary device set up as a server on the network is required for this these scripts to work. Set clientIP as your local device address and serverIP as your server address, your server will open port 11111 for requests after you run .InitServer() method on the machine. Run InitClient() method on local machine to send request to machine, which should return mbps speed. 

subject to change will be updating periodically 
