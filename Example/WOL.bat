REM Move to the directory where the .exe is
cd WakeOnLAN
REM Format - name.exe, number of trys, wait time before pinging device, MAC address of the device, IP addres of the device (IPv4 only)
WakeOnLanClient.exe 2 10000 0x:0x:0x:0x:0x xxx.xxx.xxx.xxx 