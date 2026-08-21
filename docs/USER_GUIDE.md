# Guide utilisateur

## Créer une carte

Cliquer sur **Nouvelle carte** ou sur **Ajouter** dans un groupe, puis renseigner :

- un titre personnalisable ;
- la cible à lancer ;
- les arguments éventuels ;
- le dossier de travail ;
- le groupe, la couleur, le logo et les tags ;
- autant d'accès rapides que nécessaire, avec un libellé et un chemin de dossier ;
- le lancement administrateur et la réduction de la fenêtre si nécessaire.

Le bouton **Tester la commande** permet de vérifier la cible avant d'enregistrer. Un raccourci Windows `.lnk` peut être importé avec le bouton prévu ou déposé directement dans la fenêtre.

Un dossier peut aussi être déposé dans la fenêtre : il est ajouté aux **Accès rapides**. Les variables Windows comme `%USERNAME%`, les chemins réseau `\\serveur\partage` et les chemins relatifs au dossier du catalogue sont acceptés. La carte affiche les deux premiers accès ; tous restent disponibles dans son menu `•••`.

Cliquer sur l'icône de dossier teste d'abord le chemin. S'il existe, l'Explorateur s'ouvre sans message supplémentaire ; sinon, une alerte indique le chemin inaccessible.

## Organiser

- glisser une carte sur une autre carte pour changer son ordre ;
- glisser une carte dans un autre groupe pour la déplacer ;
- utiliser le menu `•••` d'une carte pour modifier, dupliquer ou supprimer ;
- utiliser le menu d'un groupe pour le renommer, le déplacer ou le supprimer ;
- cliquer sur l'étoile d'une carte pour la placer parmi les favoris.
- cliquer sur la flèche d'un groupe pour le replier ou le déplier.

Les favoris apparaissent en premier, puis les cartes récemment utilisées, puis l'ordre manuel.

## Rechercher et filtrer

La barre de recherche inspecte les titres, descriptions, commandes et tags. Plusieurs cases peuvent rester cochées en même temps.
Elle inspecte également les libellés et chemins des accès rapides. Le filtre **Favoris uniquement** peut être combiné avec tous les autres filtres.

- dans `Version`, cocher `6.22` et `6.27` signifie `6.22 OU 6.27` ;
- ajouter `Usage = Travail` signifie aussi `ET Travail`.

Le bouton `+` de **Vues enregistrées** mémorise la recherche et tous les filtres actifs sur le poste.
La flèche située au bord droit du panneau permet de masquer ou réafficher les filtres ; cet état est conservé au prochain démarrage.

## Gérer les tags

Ouvrir `•••` puis **Gérer les tags**. Chaque tag possède :

- une catégorie, par exemple `Usage`, `Version`, `Chants`, `Année` ou `Client` ;
- un nom, par exemple `Travail`, `EP`, `SC`, `2021` ou `6.22` ;
- une couleur.

Dans l'éditeur, la catégorie est une liste modifiable : sélectionner une catégorie existante évite les variantes de saisie, mais il reste possible de saisir directement une nouvelle catégorie.

La suppression d'un tag le retire également de toutes les cartes qui l'utilisent.

## Partager un catalogue

Dans `•••`, choisir **Choisir un catalogue partagé** :

- **Oui** ouvre un catalogue existant ;
- **Non** crée une copie du catalogue actuel dans l'emplacement choisi.

Un dossier Nextcloud, OneDrive ou réseau peut être utilisé. Les favoris, récents et vues enregistrées ne sont pas partagés.

Le pied de la fenêtre indique en permanence si le catalogue est local, partagé ou indisponible, ainsi que son chemin. Cliquer sur cet état permet de changer de catalogue, de le recharger, d'ouvrir son dossier ou de revenir au catalogue local.

Lorsqu'un autre poste enregistre une modification, le catalogue est rechargé automatiquement sous quelques secondes. Si une fenêtre d'édition est ouverte, le rechargement attend sa fermeture. En cas d'indisponibilité temporaire du serveur au démarrage, le chemin partagé est conservé et le launcher peut tenter de s'y reconnecter avec **Recharger maintenant**.

## Sauvegarder ou transférer

- **Exporter le catalogue** crée une copie JSON transportable ;
- **Importer le catalogue** remplace le catalogue courant après confirmation ;
- chaque modification crée automatiquement une sauvegarde dans le dossier `Backups` situé à côté du catalogue.

Le menu **Restaurer une sauvegarde** ouvre directement ce dossier et remplace le catalogue après confirmation, tout en sauvegardant d'abord son état actuel.

## Diagnostic

Le menu **Ouvrir le journal** affiche le dossier contenant les traces de lancement et les erreurs. En cas de cible invalide, la carte reste visible mais son bouton **Lancer** est désactivé et la raison est affichée.

Le bloc **Ressources** du panneau gauche et le menu `•••` donnent accès à la page officielle des téléchargements TopSolid.
