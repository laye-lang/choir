dotnet publish .\src\Choir.Driver -r win-x64 -c Release -p:PublishAot=true 
copy .\src\Choir.Driver\bin\Release\net9.0\win-x64\publish\Choir.Driver.exe .\choir.exe
