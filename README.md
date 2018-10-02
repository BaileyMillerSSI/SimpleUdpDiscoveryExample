# Simple Udp Discovery Example
Using UDP app 1 will ask the network for any devices (app 2) available for connection. This sets up scenarios where app 1 could be looking for a list of devices that offer file sharing, etc. 

App 1 Starts - Every n seconds transmits a broadcast on thread 3 asking for available devices to return their directory listing and information for connection. Also starts thread 2 which is waiting for connections to become available that will recieve directory listings.

App 2 Starts - Begins listening for broadcasts and when received will transmit a directory listing back to the remote host.
