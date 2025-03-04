publish:
	dotnet publish ./src/Choir.Driver -r linux-x64 -c Release
	cp ./src/Choir.Driver/bin/Release/net8.0/linux-x64/publish/Choir.Driver ./choir
