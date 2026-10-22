# Architecture

## Objectif

Le domaine fonctionnel est volontairement générique : une carte représente une commande Windows, pas une version de TopSolid. La détection TopSolid ne sert qu'à créer le catalogue initial et pourra cohabiter avec d'autres fournisseurs de cartes.

## Découpage du projet

| Dossier | Responsabilité |
|---|---|
| `Models` | Données persistées, sans dépendance WPF |
| `Services` | stockage, migration, lancement, validation, ressources, import `.lnk`, mise à jour |
| `ViewModels` | recherche, filtrage, modes, ressources et projection par groupes |
| `Views` | fenêtres d'édition et de gestion |
| `Assets` | identité visuelle Idéo embarquée dans l'exécutable |

`MainWindow` orchestre les interactions utilisateur. Les règles métier et la persistance restent dans les services afin de pouvoir faire évoluer ou remplacer l'interface sans réécrire le catalogue.

## Modèle de catalogue

`LauncherCatalog` contient :

- `Groups` : sections ordonnées de l'écran ;
- `Tags` : valeurs filtrables avec catégorie, nom, couleur et ordres manuels ;
- `Cards` : apparence, ordre, modes de lancement et relations vers groupe/tags ;
- `Resources` : dossiers, fichiers ou URL utiles et leurs emplacements possibles ;
- `AuditTrail` : dernières modifications avec utilisateur et poste Windows ;
- `SchemaVersion` : version du format pour les futures migrations.

Une carte contient un nombre illimité de `LaunchProfiles`. Chaque profil porte son nom, `TargetPath`, `Arguments`, `WorkingDirectory`, `RunAsAdministrator`, `MinimizeAfterLaunch` et une liste de `QuickAccessLinks`. La carte désigne également son profil partagé par défaut. Le dernier profil effectivement utilisé reste dans les réglages locaux du poste et ne perturbe pas les autres utilisateurs.

Une ressource contient autant de `Locations` que nécessaire. Son emplacement partagé par défaut appartient au catalogue ; le dernier emplacement choisi reste local au poste. Une même ressource peut ainsi ouvrir un chemin serveur en local et un dossier synchronisé Nextcloud en distant.

Le schéma 4 ajoute les profils, ressources, ordres de catégories et historique. `CatalogService.Normalize` transforme chaque commande du schéma 2 en profil **Par défaut** sans modifier l'identifiant, le groupe, les tags, l'ordre, le logo, la couleur ni les options de la carte. L'identifiant du profil migré est dérivé de celui de la carte et reste stable avant même la première sauvegarde. Les propriétés historiques sont ensuite vidées uniquement dans le modèle normalisé afin d'éviter deux sources de vérité.

## Filtres

Pour chaque catégorie contenant des sélections, une carte doit avoir au moins un tag sélectionné. Elle doit satisfaire cette règle dans toutes les catégories actives :

```text
(tag A OU tag B de la catégorie 1)
ET
(tag C OU tag D de la catégorie 2)
```

La recherche texte s'ajoute avec `ET` et inspecte titre, description, tous les profils, noms de tags et accès rapides.

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
- le workflow Windows exécute un test de migration 0.3.2 → 0.4.0 qui vérifie les identifiants, l'ordre, le groupe, les tags, l'apparence, la commande, les options, les accès rapides et la création de la sauvegarde.

Les favoris, récents, derniers profils/emplacements utilisés, vues enregistrées, groupes repliés et état du panneau de filtres sont des réglages locaux. Les profils, ressources et accès rapides sont partagés avec le catalogue.

Le fichier JSON est lisible manuellement, mais les modifications doivent normalement passer par l'application pour bénéficier de la validation et des sauvegardes.

## Extension future

Pour ajouter la détection d'une autre application :

1. créer un service de découverte retournant des données de cartes ;
2. proposer l'import dans le catalogue sans remplacer les cartes existantes ;
3. ajouter des tags spécifiques via les catégories existantes ou nouvelles ;
4. ne pas introduire de logique propre à l'application dans `LauncherCard`.

Si le format JSON change, incrémenter `LauncherCatalog.CurrentSchemaVersion` et effectuer la migration dans `CatalogService.Normalize` avant toute sauvegarde.
