# Idéo Launcher

Application Windows destinée à centraliser les raccourcis de travail Idéo : TopSolid aujourd'hui, puis d'autres applications à mesure que le besoin évolue.

## Fonctions principales

- cartes personnalisables avec un nombre illimité de modes de lancement, par exemple **Local** et **Distant · Nextcloud** ;
- bouton scindé : lancement immédiat dans le dernier mode utilisé ou choix explicite dans le menu associé ;
- lancement normal ou administrateur, accès rapides et réduction facultative configurables par mode ;
- import d'un ou de plusieurs raccourcis Windows `.lnk` par sélection ou glisser-déposer ;
- groupes personnalisés et ordre strictement manuel des cartes, tags et catégories par glisser-déposer ;
- favoris et filtre dédié, sans déplacement automatique après un lancement ;
- recherche plein texte et filtres multi-sélection ;
- palette de lancement rapide avec `Ctrl+K` ;
- vues de filtres enregistrées, groupes et panneau latéral repliables ;
- gestion centralisée des tags et de leurs catégories ;
- ressources partagées à plusieurs emplacements, notamment le dossier des licences TopSolid en local ou via Nextcloud ;
- sélection et modification de plusieurs cartes en une seule opération ;
- validation globale du catalogue et signalement des chemins indisponibles ;
- catalogue local ou partagé sur un dossier synchronisé/réseau ;
- chemin du catalogue visible et rechargement automatique des modifications partagées ;
- historique des modifications et sauvegarde automatique des 20 dernières versions du catalogue ;
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

Les favoris, récents, dernier mode/emplacement utilisé, dimensions de fenêtre, vues enregistrées et états repliés restent propres au poste. Le catalogue (cartes, modes, ressources, groupes et tags) peut être partagé.

La mise à niveau depuis la version 0.3.2 conserve les cartes existantes. Chaque ancienne commande devient un mode **Par défaut** avec le même identifiant de carte, le même ordre, les mêmes tags, l'apparence et tous ses paramètres. Une sauvegarde du catalogue précédent est créée avant la première écriture au nouveau format.

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
