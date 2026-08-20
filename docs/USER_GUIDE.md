# Guide utilisateur

## Créer une carte

Cliquer sur **Nouvelle carte** ou sur **Ajouter** dans un groupe, puis renseigner :

- un titre personnalisable ;
- la cible à lancer ;
- les arguments éventuels ;
- le dossier de travail ;
- le groupe, la couleur, le logo et les tags ;
- le lancement administrateur et la réduction de la fenêtre si nécessaire.

Le bouton **Tester la commande** permet de vérifier la cible avant d'enregistrer. Un raccourci Windows `.lnk` peut être importé avec le bouton prévu ou déposé directement dans la fenêtre.

## Organiser

- glisser une carte sur une autre carte pour changer son ordre ;
- glisser une carte dans un autre groupe pour la déplacer ;
- utiliser le menu `•••` d'une carte pour modifier, dupliquer ou supprimer ;
- utiliser le menu d'un groupe pour le renommer, le déplacer ou le supprimer ;
- cliquer sur l'étoile d'une carte pour la placer parmi les favoris.

Les favoris apparaissent en premier, puis les cartes récemment utilisées, puis l'ordre manuel.

## Rechercher et filtrer

La barre de recherche inspecte les titres, descriptions, commandes et tags. Plusieurs cases peuvent rester cochées en même temps.

- dans `Version`, cocher `6.22` et `6.27` signifie `6.22 OU 6.27` ;
- ajouter `Usage = Travail` signifie aussi `ET Travail`.

Le bouton `+` de **Vues enregistrées** mémorise la recherche et tous les filtres actifs sur le poste.

## Gérer les tags

Ouvrir `•••` puis **Gérer les tags**. Chaque tag possède :

- une catégorie, par exemple `Usage`, `Version`, `Chants`, `Année` ou `Client` ;
- un nom, par exemple `Travail`, `EP`, `SC`, `2021` ou `6.22` ;
- une couleur.

La suppression d'un tag le retire également de toutes les cartes qui l'utilisent.

## Partager un catalogue

Dans `•••`, choisir **Choisir un catalogue partagé** :

- **Oui** ouvre un catalogue existant ;
- **Non** crée une copie du catalogue actuel dans l'emplacement choisi.

Un dossier Nextcloud, OneDrive ou réseau peut être utilisé. Les favoris, récents et vues enregistrées ne sont pas partagés.

## Sauvegarder ou transférer

- **Exporter le catalogue** crée une copie JSON transportable ;
- **Importer le catalogue** remplace le catalogue courant après confirmation ;
- chaque modification crée automatiquement une sauvegarde dans le dossier `Backups` situé à côté du catalogue.

Le menu **Restaurer une sauvegarde** ouvre directement ce dossier et remplace le catalogue après confirmation, tout en sauvegardant d'abord son état actuel.

## Diagnostic

Le menu **Ouvrir le journal** affiche le dossier contenant les traces de lancement et les erreurs. En cas de cible invalide, la carte reste visible mais son bouton **Lancer** est désactivé et la raison est affichée.
