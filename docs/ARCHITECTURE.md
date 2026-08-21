# Architecture

## Objectif

Le domaine fonctionnel est volontairement générique : une carte représente une commande Windows, pas une version de TopSolid. La détection TopSolid ne sert qu'à créer le catalogue initial et pourra cohabiter avec d'autres fournisseurs de cartes.

## Découpage du projet

| Dossier | Responsabilité |
|---|---|
| `Models` | Données persistées, sans dépendance WPF |
| `Services` | stockage, lancement, validation, import `.lnk`, mise à jour |
| `ViewModels` | recherche, filtrage, favoris, récents et projection par groupes |
| `Views` | fenêtres d'édition et de gestion |
| `Assets` | identité visuelle Idéo embarquée dans l'exécutable |

`MainWindow` orchestre les interactions utilisateur. Les règles métier et la persistance restent dans les services afin de pouvoir faire évoluer ou remplacer l'interface sans réécrire le catalogue.

## Modèle de catalogue

`LauncherCatalog` contient :

- `Groups` : sections ordonnées de l'écran ;
- `Tags` : valeurs filtrables avec catégorie, nom et couleur ;
- `Cards` : commandes, apparence, comportement et relations vers groupe/tags ;
- `SchemaVersion` : version du format pour les futures migrations.

Une carte contient notamment `TargetPath`, `Arguments`, `WorkingDirectory`, `RunAsAdministrator`, `MinimizeAfterLaunch` et une liste de `QuickAccessLinks`. Chaque accès rapide ne stocke qu'un libellé et un chemin : la résolution et la validation restent dans le ViewModel au moment de l'affichage ou de l'ouverture. Aucun script avant/après lancement ni suivi d'instances n'est exécuté.

Le schéma 2 ajoute `QuickAccessLinks`. `CatalogService.Normalize` initialise cette liste lors de l'ouverture d'un ancien catalogue ; la migration est donc ascendante et ne modifie pas les cartes existantes.

## Filtres

Pour chaque catégorie contenant des sélections, une carte doit avoir au moins un tag sélectionné. Elle doit satisfaire cette règle dans toutes les catégories actives :

```text
(tag A OU tag B de la catégorie 1)
ET
(tag C OU tag D de la catégorie 2)
```

La recherche texte s'ajoute avec `ET` et inspecte titre, description, cible, arguments, noms de tags et accès rapides.

## Persistance et sûreté

- les écritures JSON utilisent un fichier temporaire puis un remplacement atomique ;
- une copie du catalogue courant est créée avant chaque modification ;
- les 20 sauvegardes les plus récentes sont conservées ;
- les logos choisis sont copiés dans `Assets` à côté du catalogue et référencés par un chemin relatif ;
- une écriture est refusée si le fichier partagé a été modifié par un autre poste depuis son chargement ;
- la date d'écriture du catalogue actif est contrôlée périodiquement et déclenche un rechargement automatique hors fenêtre modale ;
- une indisponibilité temporaire du partage ne supprime pas le chemin configuré dans les préférences locales ;
- les erreurs non gérées sont journalisées ;
- les cibles sont validées avant enregistrement et avant lancement ;
- les réglages personnels sont séparés du catalogue partageable.

Les favoris, vues enregistrées, groupes repliés et état du panneau de filtres sont des réglages locaux. Les accès rapides appartiennent aux cartes et sont donc partagés avec le catalogue.

Le fichier JSON est lisible manuellement, mais les modifications doivent normalement passer par l'application pour bénéficier de la validation et des sauvegardes.

## Extension future

Pour ajouter la détection d'une autre application :

1. créer un service de découverte retournant des données de cartes ;
2. proposer l'import dans le catalogue sans remplacer les cartes existantes ;
3. ajouter des tags spécifiques via les catégories existantes ou nouvelles ;
4. ne pas introduire de logique propre à l'application dans `LauncherCard`.

Si le format JSON change, incrémenter `LauncherCatalog.CurrentSchemaVersion` et effectuer la migration dans `CatalogService.Normalize` avant toute sauvegarde.
