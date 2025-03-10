publish:
	dotnet publish ./src/Choir.Driver -r linux-x64 -c Release -p:PublishAot=true 
	cp ./src/Choir.Driver/bin/Release/net9.0/linux-x64/publish/Choir.Driver ./choir
