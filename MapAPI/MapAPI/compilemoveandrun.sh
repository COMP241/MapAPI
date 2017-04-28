cd /home/cameron/Documents/MapAPI
dotnet restore
dotnet publish
cd /home/cameron/Documents/MapAPI/bin/Debug/netcoreapp1.1/publish
sudo rm -rf /var/www/*
sudo cp -r ./* /var/www
cd /var/www
dotnet MapAPI.dll