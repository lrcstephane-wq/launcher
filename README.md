# Launcher TopSolid — V0

Launcher Windows simple pour démarrer la version souhaitée de TopSolid'Wood V6.

## Fonctionnalités

- détection automatique des installations dans `C:\Missler\V6xx\bin` ;
- affichage de toutes les versions détectées ;
- démarrage d'une nouvelle instance à chaque clic ;
- réduction automatique du launcher dans la barre des tâches après le lancement ;
- bouton d'actualisation ;
- compilation automatique d'un exécutable Windows autonome avec GitHub Actions.
- recherche automatique des nouvelles versions publiées sur GitHub ;
- téléchargement, remplacement et redémarrage automatiques du launcher.

## Récupérer l'exécutable

1. Ouvrir l'onglet **Actions** du dépôt.
2. Ouvrir la dernière exécution **Build Windows** terminée avec succès.
3. Télécharger l'artifact **Launcher-Windows-x64**.
4. Décompresser puis lancer `Launcher.exe`.

Windows SmartScreen peut afficher un avertissement tant que l'application n'est pas signée.

## Compilation locale

Prérequis : Windows et SDK .NET 8.

```powershell
dotnet publish .\src\Ideo.TopSolidLauncher\Ideo.TopSolidLauncher.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true `
  -o .\publish
```
