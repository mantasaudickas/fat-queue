mkdir publish

rem .nuget\Nuget.exe pack Messenger.Core\FatQueue.Messenger.Core.csproj -IncludeReferencedProjects -o publish -symbols -prop Configuration=Release
rem .nuget\Nuget.exe pack Messenger.MsSql\FatQueue.Messenger.MsSql.csproj -o publish -symbols -prop Configuration=Release

.nuget\Nuget.exe pack Messenger.Core\FatQueue.Messenger.Core.csproj -IncludeReferencedProjects -o publish -symbols -prop Configuration=Release
.nuget\Nuget.exe pack Messenger.MsSql\FatQueue.Messenger.MsSql.csproj -o publish -symbols -prop Configuration=Release
.nuget\Nuget.exe pack Messenger.PostgreSql\FatQueue.Messenger.PostgreSql.csproj -o publish -symbols -prop Configuration=Release

pause