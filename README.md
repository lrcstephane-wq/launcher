# Idéo Launcher

Application Windows destinée à centraliser les raccourcis de travail Idéo : TopSolid aujourd'hui, puis d'autres applications à mesure que le besoin évolue.

## Fonctions principales

- cartes personnalisables : titre, description, cible, arguments, dossier de travail, logo, couleur et tags ;
- lancement normal ou administrateur, avec réduction facultative du launcher ;
- import d'un raccourci Windows `.lnk` par sélection ou glisser-déposer ;
- groupes personnalisés et réorganisation des cartes par glisser-déposer ;
- favoris et classement des éléments récemment lancés ;
- recherche plein texte et filtres multi-sélection ;
- vues de filtres enregistrées ;
- gestion centralisée des tags et de leurs catégories ;
- catalogue local ou partagé sur un dossier synchronisé/réseau ;
- sauvegarde automatique des 20 dernières versions du catalogue ;
- import et export JSON ;
- mise à jour automatique depuis les releases GitHub.

La première ouverture crée automatiquement des cartes à partir des installations détectées dans `C:\Missler\V6xx\bin`. Les cartes créées ensuite ne sont pas limitées à TopSolid.

## Règles des filtres

- plusieurs tags sélectionnés dans une même catégorie sont combinés avec **OU** ;
- les différentes catégories actives sont combinées avec **ET**.

Exemple : `Version = 6.22 OU 6.27` avec `Usage = Travail` affiche les cartes de travail correspondant à l'une de ces deux versions.

## Données utilisateur

- catalogue par défaut : `%APPDATA%\Ideo\Launcher\catalog.json` ;
- préférences personnelles : `%LOCALAPPDATA%\Ideo\Launcher\settings.json` ;
- sauvegardes : dossier `Backups` situé à côté du catalogue ;
- journal : `%LOCALAPPDATA%\Ideo\TopSolidLauncher\launcher.log`.

Les favoris, récents, dimensions de fenêtre et vues enregistrées restent propres au poste. Le catalogue (cartes, groupes et tags) peut être partagé.

## Télécharger l'exécutable

1. ouvrir l'onglet **Releases** du dépôt ;
2. télécharger `Launcher.exe` depuis la dernière version ;
3. conserver l'exécutable dans un dossier stable puis le lancer.

Windows SmartScreen peut afficher un avertissement tant que l'application n'est pas signée.

## Compilation locale

Prérequis : Windows et SDK .NET 8.

```powershell
dotnet publish .\src\Ideo.TopSolidLauncher\Ideo.TopSolidLauncher.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true `
  -o .\publish
```

## Documentation

- [Guide utilisateur](docs/USER_GUIDE.md)
- [Architecture et format des données](docs/ARCHITECTURE.md)
- [Plan de tests](docs/TEST_PLAN.md)
- [Procédure de publication](docs/RELEASE.md)
- [Historique des versions](docs/CHANGELOG.md)
